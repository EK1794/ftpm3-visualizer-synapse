import React from 'react';

export type TrackHistoryItem = { id: number, title: string, artist: string, album: string, time: string, base64Image?: string | null };

export function HistoryPanel({ history }: { history: TrackHistoryItem[] }) {

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
                {history.map((track, i) => (
                    <div key={track.id} className="bg-black/20 hover:bg-black/40 border border-synapse-700/30 p-3 rounded-lg flex items-center gap-3 transition-colors group cursor-default">
                        <div className="text-xs font-mono text-synapse-700 w-4">{history.length - i}.</div>

                        <div className="w-10 h-10 bg-synapse-800 rounded border border-synapse-700 flex-shrink-0 flex items-center justify-center">
                            <span className="text-[8px] text-synapse-700">IMG</span>
                        </div>

                        <div className="flex-1 min-w-0">
                            <div className="text-sm font-bold text-gray-200 truncate group-hover:text-synapse-accent transition-colors">{track.title}</div>
                            <div className="text-xs text-gray-400 truncate">{track.artist}</div>
                        </div>

                        <div className="text-[10px] text-gray-500 font-mono self-start mt-1">
                            {track.time}
                        </div>
                    </div>
                ))}

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
