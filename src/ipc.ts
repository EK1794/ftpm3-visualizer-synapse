import { invoke } from '@tauri-apps/api/core';
import { listen } from '@tauri-apps/api/event';

export type CommandEnvelope = {
    command: string;
    payload: any;
};

export async function sendCommandToSidecar(command: string, payload: any) {
    const envelope: CommandEnvelope = { command, payload };
    try {
        await invoke('send_to_sidecar', { message: JSON.stringify(envelope) });
    } catch (err) {
        console.error('Failed to send sidecar command:', err);
    }
}

export function listenToSidecarEvents(onEvent: (data: any) => void) {
    return listen<string>('sidecar-event', (event) => {
        try {
            const parsed = JSON.parse(event.payload);
            onEvent(parsed);
        } catch (err) {
            console.warn('Failed to parse sidecar event:', event.payload);
        }
    });
}

// Concrete IPC actions
export const ipcActions = {
    getStatus: () => sendCommandToSidecar('GetStatus', {}),
    getDevices: () => sendCommandToSidecar('GetCameraDevices', {}),
    assignCamera: (slotIndex: number, deviceId: string) => sendCommandToSidecar('AssignCamera', { slotIndex, deviceId }),
    triggerOcr: (slotIndex: number) => sendCommandToSidecar('TriggerOcr', { slotIndex }),
    updateStaging: (title: string, artist: string, album: string, isImageForced: boolean) => {
        sendCommandToSidecar('UpdateStagingBuffer', { title, artist, album, isImageForced });
    },
    triggerOutput: (title: string, artist: string, album: string, base64Image: string | null, isImageForced: boolean) => {
        sendCommandToSidecar('TriggerOutput', { title, artist, album, base64Image, isImageForced });
    }
};
