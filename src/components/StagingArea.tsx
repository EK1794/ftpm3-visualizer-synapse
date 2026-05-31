import React from 'react';

type Props = {
    title: string;
    setTitle: (v: string) => void;
    artist: string;
    setArtist: (v: string) => void;
    album: string;
    setAlbum: (v: string) => void;
    forceImage: boolean;
    setForceImage: (v: boolean) => void;
    onTriggerOutput: () => void;
};

export function StagingArea({ title, setTitle, artist, setArtist, album, setAlbum, forceImage, setForceImage, onTriggerOutput }: Props) {
    return (
        <div className="flex flex-col h-full">
            <h2 className="text-sm font-semibold tracking-widest text-gray-400 uppercase mb-6">Staging Area (Next Track)</h2>

            <div className="flex-1 overflow-y-auto pr-4">
                <div className="bg-synapse-800 rounded-xl p-6 shadow-2xl border border-synapse-700/50 backdrop-blur-sm">
                    <div className="flex gap-6 mb-8">
                        {/* Artwork Placeholder */}
                        <div className="w-48 h-48 bg-black/40 rounded-lg border-2 border-dashed border-synapse-700 flex items-center justify-center relative overflow-hidden group">
                            <span className="text-synapse-700 font-medium">ARTWORK</span>
                        </div>

                        <div className="flex-1 flex flex-col gap-4 justify-center">
                            <div className="flex flex-col">
                                <label className="text-xs text-synapse-accent font-bold tracking-wider mb-1">TITLE</label>
                                <textarea
                                    value={title}
                                    onChange={e => setTitle(e.target.value)}
                                    className="bg-transparent border-b-2 border-synapse-700 focus:border-synapse-accent text-2xl font-bold p-1 outline-none transition-colors resize-none h-16"
                                />
                            </div>

                            <div className="flex flex-col">
                                <label className="text-xs text-synapse-accent font-bold tracking-wider mb-1">ARTIST</label>
                                <input
                                    type="text"
                                    value={artist}
                                    onChange={e => setArtist(e.target.value)}
                                    className="bg-transparent border-b-2 border-synapse-700 focus:border-synapse-accent text-xl text-gray-300 p-1 outline-none transition-colors"
                                />
                            </div>

                            <div className="flex flex-col">
                                <label className="text-xs text-synapse-accent font-bold tracking-wider mb-1">ALBUM</label>
                                <input
                                    type="text"
                                    value={album}
                                    onChange={e => setAlbum(e.target.value)}
                                    className="bg-transparent border-b-2 border-synapse-700 focus:border-synapse-accent text-lg text-gray-400 p-1 outline-none transition-colors"
                                />
                            </div>
                        </div>
                    </div>

                    <div className="flex items-center justify-between mt-8 p-4 bg-black/20 rounded-lg">
                        <label className="flex items-center gap-3 cursor-pointer group">
                            <div className="relative flex items-center">
                                <input type="checkbox" className="sr-only" checked={forceImage} onChange={e => setForceImage(e.target.checked)} />
                                <div className={`w-10 h-6 bg-synapse-900 rounded-full border border-synapse-700 transition-colors ${forceImage ? 'bg-synapse-accent/30 border-synapse-accent' : ''}`}></div>
                                <div className={`absolute left-1 top-1 bg-gray-400 w-4 h-4 rounded-full transition-transform ${forceImage ? 'transform translate-x-4 bg-synapse-accent' : ''}`}></div>
                            </div>
                            <span className="text-sm font-medium text-gray-300 group-hover:text-white transition-colors">Force Image Mode (Safety)</span>
                        </label>
                    </div>
                </div>
            </div>

            {/* Trigger Button Footer */}
            <div className="mt-6 pt-6 border-t border-synapse-700/50">
                <button
                    onClick={onTriggerOutput}
                    className="w-full bg-gradient-to-r from-synapse-700 to-synapse-800 hover:from-synapse-accent hover:to-red-600 text-white font-black text-2xl py-6 rounded-xl shadow-[0_10px_30px_rgba(233,69,96,0.3)] hover:shadow-[0_10px_40px_rgba(233,69,96,0.5)] active:scale-95 transition-all outline-none border border-synapse-accent/50 tracking-[0.2em]"
                >
                    MIDI GO / MANUAL OUTPUT
                </button>
            </div>
        </div>
    );
}
