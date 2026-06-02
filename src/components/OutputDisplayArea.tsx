import { useState } from 'react';
import { TrackHistoryItem } from './HistoryPanel';

type OutputDisplayAreaProps = {
    track: TrackHistoryItem | undefined;
    displayMode: 'text' | 'image';
};

const CHROMA_COLORS = [
    { name: 'None', value: 'transparent' },
    { name: 'Green', value: '#00FF00' },
    { name: 'Magenta', value: '#FF00FF' },
    { name: 'Black', value: '#000000' }
];

export function OutputDisplayArea({ track, displayMode }: OutputDisplayAreaProps) {
    const [chromaIndex, setChromaIndex] = useState(0);
    const bgStyle = { backgroundColor: CHROMA_COLORS[chromaIndex].value };

    const cycleChroma = () => setChromaIndex((prev) => (prev + 1) % CHROMA_COLORS.length);

    return (
        <div className="flex flex-col h-1/2 p-4 border-b border-synapse-700/50">
            <div className="flex items-center justify-between mb-2">
                <h2 className="text-sm font-semibold tracking-widest text-gray-400 uppercase">Output Display</h2>
                <div className="flex gap-2 text-xs">
                    <button 
                        onClick={cycleChroma}
                        className="px-2 py-1 bg-synapse-800 hover:bg-synapse-700 text-gray-300 rounded border border-synapse-700 transition-colors"
                    >
                        BG: {CHROMA_COLORS[chromaIndex].name}
                    </button>
                    <span className="px-2 py-1 bg-synapse-800 text-synapse-accent rounded border border-synapse-700">
                        Mode: {displayMode.toUpperCase()}
                    </span>
                </div>
            </div>

            <div 
                className="flex-1 rounded-xl overflow-hidden relative border border-synapse-600/50 shadow-2xl flex items-center justify-center transition-colors duration-300"
                style={bgStyle}
            >
                {!track ? (
                    <div className="text-gray-500 font-mono tracking-wider">NO OUTPUT TRACK</div>
                ) : displayMode === 'image' ? (
                    track.base64Image ? (
                        <img src={track.base64Image} alt="Artwork" className="w-full h-full object-contain" />
                    ) : (
                        <div className="text-gray-500 font-mono">NO IMAGE DATA</div>
                    )
                ) : (
                    <div className="w-full h-full flex flex-col justify-end p-8 bg-gradient-to-t from-black/80 to-transparent">
                        <div className="flex items-end gap-6">
                            {track.base64Image && (
                                <img src={track.base64Image} alt="Artwork" className="w-24 h-24 rounded-md shadow-lg border border-white/20 object-cover" />
                            )}
                            <div className="flex flex-col gap-1 drop-shadow-md">
                                {track.title && <h1 className="text-4xl font-extrabold text-white tracking-tight">{track.title}</h1>}
                                {track.artist && <h2 className="text-2xl font-medium text-gray-200">{track.artist}</h2>}
                                {track.album && <h3 className="text-sm font-light text-synapse-accent tracking-widest uppercase">{track.album}</h3>}
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}
