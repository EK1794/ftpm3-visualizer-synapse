use std::sync::Mutex;
use tauri::{Manager, Emitter};
use tauri_plugin_shell::ShellExt;
use tauri_plugin_shell::process::{CommandEvent, CommandChild};

struct SidecarState {
    child: Mutex<Option<CommandChild>>,
}

#[tauri::command]
fn send_to_sidecar(state: tauri::State<SidecarState>, message: String) -> Result<(), String> {
    if let Some(child) = state.child.lock().unwrap().as_mut() {
        child.write(format!("{}\n", message).as_bytes()).map_err(|e| e.to_string())?;
    }
    Ok(())
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_shell::init())
        .setup(|app| {
            let sidecar_command = app.shell().sidecar("synapse-core").expect("Failed to create sidecar command");
            let (mut rx, child) = sidecar_command.spawn().expect("Failed to spawn sidecar");
            
            app.manage(SidecarState {
                child: Mutex::new(Some(child)),
            });

            let app_handle = app.handle().clone();
            tauri::async_runtime::spawn(async move {
                while let Some(event) = rx.recv().await {
                    if let CommandEvent::Stdout(line) = event {
                        if let Ok(line_str) = String::from_utf8(line) {
                            let _ = app_handle.emit("sidecar-event", line_str);
                        }
                    } else if let CommandEvent::Stderr(line) = event {
                        if let Ok(line_str) = String::from_utf8(line) {
                            println!("Sidecar Stderr: {}", line_str);
                        }
                    }
                }
            });
            Ok(())
        })
        .invoke_handler(tauri::generate_handler![send_to_sidecar])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
