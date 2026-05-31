import React from 'react';

type Props = {
    slotIndex: number;
    devices: { id: string, name: string }[];
    currentFrame?: string;
    onAssign: (deviceId: string) => void;
    onCapture: () => void;
};

export function CameraPreview({ slotIndex, devices, currentFrame, onAssign, onCapture }: Props) {
    return (
        <div className="flex flex-col bg-synapse-800 rounded-lg overflow-hidden border border-synapse-700/50 shadow-lg">
            <div className="p-2 border-b border-synapse-700/50 bg-black/20">
                <select
                    className="w-full bg-synapse-900 text-sm p-1.5 rounded text-gray-300 border border-synapse-700 focus:outline-none focus:border-synapse-accent"
                    onChange={(e) => onAssign(e.target.value)}
                    defaultValue=""
                >
                    <option value="" disabled>Select Device [{slotIndex}]</option>
                    {devices.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
                </select>
            </div>

            <div className="relative aspect-video bg-black/60 flex items-center justify-center group overflow-hidden">
                {!currentFrame && <span className="text-synapse-700 font-mono text-xs z-0 pointer-events-none">NO SIGNAL</span>}
                {currentFrame && <img src={currentFrame} alt={`Camera ${slotIndex}`} className="absolute inset-0 w-full h-full object-cover z-10" />}

                <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-transparent to-transparent opacity-0 group-hover:opacity-100 transition-opacity z-20 flex items-end justify-center pb-3">
                    <button
                        onClick={onCapture}
                        className="bg-synapse-accent hover:bg-red-500 text-white text-xs font-bold py-1.5 px-4 rounded-full shadow-[0_0_10px_rgba(233,69,96,0.5)] transition-all transform hover:scale-105 active:scale-95"
                    >
                        CAPTURE OCR
                    </button>
                </div>
            </div>
        </div>
    );
}
