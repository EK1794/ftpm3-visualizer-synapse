import { useEffect, useState, useRef } from 'react';
import { ipcActions, listenToSidecarEvents } from './ipc';
import { CameraPreview } from './components/CameraPreview';
import { StagingArea } from './components/StagingArea';
import { HistoryPanel, TrackHistoryItem } from './components/HistoryPanel';
import { OutputDisplayArea } from './components/OutputDisplayArea';


function App() {
  const [status, setStatus] = useState<string>('Initializing...');
  const [devices, setDevices] = useState<{ id: string, name: string }[]>([]);
  const [frames, setFrames] = useState<Record<number, string>>({});

  // Lifted Staging State
  const [stagingTitle, setStagingTitle] = useState('');
  const [stagingArtist, setStagingArtist] = useState('');
  const [stagingAlbum, setStagingAlbum] = useState('');
  const [forceImage, setForceImage] = useState(false);
  const [history, setHistory] = useState<TrackHistoryItem[]>([]);
  const [historyPointer, setHistoryPointer] = useState<number>(0);
  const [displayMode, setDisplayMode] = useState<'text' | 'image'>('text');

  const stagingRef = useRef({ title: '', artist: '', album: '', forceImage: false });
  const historyRef = useRef<TrackHistoryItem[]>([]);

  useEffect(() => {
    stagingRef.current = { title: stagingTitle, artist: stagingArtist, album: stagingAlbum, forceImage };
  }, [stagingTitle, stagingArtist, stagingAlbum, forceImage]);

  useEffect(() => {
    historyRef.current = history;
  }, [history]);

  const handleTriggerOutput = () => {
    const current = stagingRef.current;
    setHistory(prev => [{
      id: Date.now(),
      title: current.title || 'Unknown Title',
      artist: current.artist || 'Unknown Artist',
      album: current.album || '',
      time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    }, ...prev]);
    setHistoryPointer(0);
  };

  const handleUpdateHistoryItem = (id: number, updates: Partial<TrackHistoryItem>) => {
    setHistory(prev => prev.map(item => item.id === id ? { ...item, ...updates } : item));
  };

  useEffect(() => {
    const unlistenPromise = listenToSidecarEvents((data) => {
      // Issue #9: Accept both 'sidecar_ready' (legacy push) and 'StatusResponse' (pull handshake)
      if (data.event === 'sidecar_ready' || data.event === 'StatusResponse') {
        setStatus('Sidecar Connected');
        // Fetch devices on startup
        ipcActions.getDevices();
      } else if (data.event === 'CameraDevicesResult') {
        setDevices(data.devices || []);
      } else if (data.event === 'CameraFrameEvent') {
        setFrames(prev => ({ ...prev, [data.slotIndex]: data.base64Image }));
      } else if (data.event === 'OcrResultEvent') {
        setStagingTitle(data.title || '');
        setStagingArtist(data.artist || '');
        setStagingAlbum(data.album || '');
      } else if (data.event === 'MidiAction') {
        const { action } = data.payload;
        if (action === 'go') {
          handleTriggerOutput();
        } else if (action === 'undo') {
          setHistoryPointer(prev => Math.min(prev + 1, Math.max(0, historyRef.current.length - 1)));
        } else if (action === 'redo') {
          setHistoryPointer(prev => Math.max(0, prev - 1));
        } else if (action === 'toggle') {
          setDisplayMode(prev => prev === 'text' ? 'image' : 'text');
        }
      } else if (data.event === 'MidiMessageEvent') {
        // Legacy event (ignored now that backend is updated)
      } else if (data.event === 'ack') {
        console.log('Command ACK:', data);
      } else if (data.event === 'error') {
        setStatus(`Error: ${data.message}`);
      } else {
        console.log('Unhandled Sidecar Event:', data);
      }
    });

    // Issue #9: After listener is registered, explicitly ask Sidecar for its status.
    // This eliminates the race condition where sidecar_ready was emitted
    // before the listener was set up.
    unlistenPromise.then(() => {
      ipcActions.getStatus();
    });

    return () => {
      unlistenPromise.then(unlisten => {
        if (unlisten) unlisten();
      });
    };
  }, []);

  return (
    <div className="flex flex-col h-screen text-gray-100 bg-synapse-900 border-t-4 border-synapse-accent">
      {/* Top Header */}
      <header className="flex items-center justify-between px-6 py-3 bg-synapse-800 shadow-md">
        <div className="flex items-center gap-3">
          <div className="w-3 h-3 rounded-full bg-synapse-accent animate-pulse"></div>
          <h1 className="text-xl font-bold tracking-wider text-white">SYNAPSE <span className="text-sm font-normal text-gray-400">v2.0</span></h1>
        </div>
        <div className="text-sm font-mono text-gray-400">
          Status: <span className={status === 'Sidecar Connected' ? 'text-green-400' : 'text-yellow-400'}>{status}</span>
        </div>
      </header>

      {/* Main Layout */}
      <main className="flex flex-1 overflow-hidden">
        {/* Left Column: Cameras */}
        <section className="flex flex-col w-1/4 p-4 gap-4 overflow-y-auto border-r border-synapse-700/50">
          <h2 className="text-sm font-semibold tracking-widest text-gray-400 uppercase">Input Sources</h2>
          <CameraPreview slotIndex={1} devices={devices} currentFrame={frames[1]} onAssign={(id) => ipcActions.assignCamera(1, id)} onCapture={() => ipcActions.triggerOcr(1)} />
          <CameraPreview slotIndex={2} devices={devices} currentFrame={frames[2]} onAssign={(id) => ipcActions.assignCamera(2, id)} onCapture={() => ipcActions.triggerOcr(2)} />
          <CameraPreview slotIndex={3} devices={devices} currentFrame={frames[3]} onAssign={(id) => ipcActions.assignCamera(3, id)} onCapture={() => ipcActions.triggerOcr(3)} />
          <CameraPreview slotIndex={4} devices={devices} currentFrame={frames[4]} onAssign={(id) => ipcActions.assignCamera(4, id)} onCapture={() => ipcActions.triggerOcr(4)} />
        </section>

        {/* Center Column: Staging */}
        <section className="flex flex-col flex-1 p-6 relative">
          <StagingArea
            title={stagingTitle} setTitle={setStagingTitle}
            artist={stagingArtist} setArtist={setStagingArtist}
            album={stagingAlbum} setAlbum={setStagingAlbum}
            forceImage={forceImage} setForceImage={setForceImage}
            onTriggerOutput={handleTriggerOutput}
          />
        </section>

        {/* Right Column: History & Output Display */}
        <section className="flex flex-col w-1/3 bg-synapse-800/30 border-l border-synapse-700/50">
          <OutputDisplayArea 
            track={history[historyPointer]} 
            displayMode={displayMode} 
          />
          <HistoryPanel 
            history={history} 
            historyPointer={historyPointer}
            onUpdateItem={handleUpdateHistoryItem} 
          />
        </section>
      </main>
    </div>
  );
}

export default App;
