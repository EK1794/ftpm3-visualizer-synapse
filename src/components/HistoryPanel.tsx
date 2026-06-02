import { useState } from 'react';

export type TrackHistoryItem = { id: number, title: string, artist: string, album: string, time: string, base64Image?: string | null };

type HistoryPanelProps = { 
    history: TrackHistoryItem[];
    historyPointer: number;
    onUpdateItem: (id: number, updates: Partial<TrackHistoryItem>) => void;
};

export function HistoryPanel({ history, historyPointer, onUpdateItem }: HistoryPanelProps) {
    const [editingId, setEditingId] = useState<number | null>(null);
    const [editData, setEditData] = useState<{title: string, artist: string, album: string}>({ title: '', artist: '', album: '' });

    const handleDoubleClick = (track: TrackHistoryItem) => {
        setEditingId(track.id);
        setEditData({ title: track.title, artist: track.artist, album: track.album });
    };

    const handleSave = (id: number) => {
        onUpdateItem(id, editData);
        setEditingId(null);
    };

    const handleKeyDown = (e: React.KeyboardEvent, id: number) => {
        if (e.key === 'Enter') handleSave(id);
        if (e.key === 'Escape') setEditingId(null);
    };

    return (
        <div className="flex flex-col h-full p-4">
            <div className="flex items-center justify-between mb-4">
                <h2 className="text-sm font-semibold tracking-widest text-gray-400 uppercase">History / Output</h2>
                <div className="flex gap-2 text-xs">
                    <button className="text-synapse-accent hover:text-white transition-colors">Clear</button>
                    <button className="text-gray-400 hover:text-white transition-colors">Export</button>
                </div>
            </div>

            <div className="flex-1 overflow-y-auto space-y-2 pr-2">
                {history.map((track, i) => {
                    const isActive = i === historyPointer;
                    return (
                    <div 
                        key={track.id} 
                        onDoubleClick={() => handleDoubleClick(track)}
                        className={`bg-black/20 hover:bg-black/40 border p-3 rounded-lg flex items-center gap-3 transition-colors group cursor-default
                            ${isActive ? 'border-synapse-accent shadow-[0_0_10px_rgba(0,255,255,0.2)]' : 'border-synapse-700/30'}`}
                    >
                        <div className={`text-xs font-mono w-4 ${isActive ? 'text-synapse-accent' : 'text-synapse-700'}`}>
                            {isActive ? '▶' : `${history.length - i}.`}
                        </div>

                        <div className="w-10 h-10 bg-synapse-800 rounded border border-synapse-700 flex-shrink-0 flex items-center justify-center overflow-hidden">
                            {track.base64Image ? (
                                <img src={track.base64Image} className="w-full h-full object-cover" alt="Art" />
                            ) : (
                                <span className="text-[8px] text-synapse-700">IMG</span>
                            )}
                        </div>

                        <div className="flex-1 min-w-0">
                            {editingId === track.id ? (
                                <div className="flex flex-col gap-1">
                                    <input 
                                        autoFocus
                                        value={editData.title}
                                        onChange={e => setEditData({...editData, title: e.target.value})}
                                        onKeyDown={e => handleKeyDown(e, track.id)}
                                        onBlur={() => handleSave(track.id)}
                                        className="text-sm font-bold bg-black/50 text-white border border-synapse-accent/50 rounded px-1 outline-none"
                                    />
                                    <input 
                                        value={editData.artist}
                                        onChange={e => setEditData({...editData, artist: e.target.value})}
                                        onKeyDown={e => handleKeyDown(e, track.id)}
                                        onBlur={() => handleSave(track.id)}
                                        className="text-xs bg-black/50 text-gray-300 border border-synapse-accent/50 rounded px-1 outline-none"
                                    />
                                </div>
                            ) : (
                                <>
                                    <div className="text-sm font-bold text-gray-200 truncate group-hover:text-synapse-accent transition-colors">
                                        {track.title || 'Unknown'}
                                    </div>
                                    <div className="text-xs text-gray-400 truncate">
                                        {track.artist}
                                    </div>
                                </>
                            )}
                        </div>

                        <div className="text-[10px] text-gray-500 font-mono self-start mt-1">
                            {track.time}
                        </div>
                    </div>
                )})}

                {history.length === 0 && (
                    <div className="text-center text-sm text-synapse-700 mt-10">
                        No tracks in output history yet.
                    </div>
                )}
            </div>

            <div className="mt-4 pt-4 border-t border-synapse-700/50 flex justify-end gap-2 text-xs">
                <button className="px-3 py-1.5 bg-synapse-800 hover:bg-synapse-700 text-gray-300 rounded border border-synapse-700 transition-colors">Settings</button>
                <button className="px-3 py-1.5 bg-synapse-800 hover:bg-synapse-700 text-gray-300 rounded border border-synapse-700 transition-colors">Layout Builder</button>
            </div>
        </div>
    );
}
