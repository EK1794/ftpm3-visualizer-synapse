import { useEffect, useState, useRef } from 'react';
import { ipcActions, listenToSidecarEvents } from './ipc';
import { CameraPreview } from './components/CameraPreview';
import { StagingArea } from './components/StagingArea';
import { HistoryPanel, TrackHistoryItem } from './components/HistoryPanel';


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

  const stagingRef = useRef({ title: '', artist: '', album: '', forceImage: false });

  useEffect(() => {
    stagingRef.current = { title: stagingTitle, artist: stagingArtist, album: stagingAlbum, forceImage };
  }, [stagingTitle, stagingArtist, stagingAlbum, forceImage]);

  const handleTriggerOutput = () => {
    const current = stagingRef.current;
    ipcActions.triggerOutput(current.title, current.artist, current.album, null, current.forceImage);
    setHistory(prev => [{
      id: Date.now(),
      title: current.title || 'Unknown Title',
      artist: current.artist || 'Unknown Artist',
      album: current.album || '',
      time: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    }, ...prev]);
  };

  useEffect(() => {
    const unlistenPromise = listenToSidecarEvents((data) => {
      if (data.event === 'sidecar_ready') {
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
      } else if (data.event === 'MidiMessageEvent') {
        if (data.isNoteOn) {
          handleTriggerOutput();
        }
      } else if (data.event === 'ack') {
        console.log('Command ACK:', data);
      } else if (data.event === 'error') {
        setStatus(`Error: ${data.message}`);
      } else {
        console.log('Unhandled Sidecar Event:', data);
      }
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

        {/* Right Column: History & settings */}
        <section className="flex flex-col w-1/4 bg-synapse-800/30 border-l border-synapse-700/50">
          <HistoryPanel history={history} />
        </section>
      </main>
    </div>
  );
}

export default App;
