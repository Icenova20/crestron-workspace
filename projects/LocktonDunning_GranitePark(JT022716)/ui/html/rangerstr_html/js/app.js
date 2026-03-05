/**
 * Rangers Training Room - Crestron Control Logic
 * Mapped for CH5 (Crestron HTML5)
 */

// --- JOIN MAP CONFIGURATION ---
const JOINS = {
    // Sources (Digital)
    Source: {
        Off: 10,
        Wallplate: 11,
        Cable1: 12,
        Cable2: 13,
        Appspace: 14,
        Clickshare: 15
    },
    // Volume (Analog: 0-65535)
    Volume: {
        MasterLevel: 1, // Analog
        MasterMute: 1   // Digital (Toggle/Fb)
    },
    // Camera Control (Digital)
    Camera: {
        SelectPodium: 21,
        SelectRoom: 22,
        TiltUp: 23,
        TiltDown: 24,
        PanLeft: 25,
        PanRight: 26,
        ZoomIn: 27,
        ZoomOut: 28,
        Preset1: 31,
        Preset2: 32,
        Preset3: 33,
        Preset4: 34,
        Preset5: 35,
        PresetSaved: 39 // Feedback
    },
    // Cable Box Control (Digital) - Shared Buttons, Logic on Processor handles which box is controlled
    Cable: {
        Up: 41, Down: 42, Left: 43, Right: 44, OK: 45,
        Num1: 51, Num2: 52, Num3: 53, Num4: 54, Num5: 55, 
        Num6: 56, Num7: 57, Num8: 58, Num9: 59, Num0: 60,
        ChUp: 61, ChDown: 62,
        Menu: 63, Guide: 64, Info: 65, Exit: 66,
        PrevChannel: 67
    },
    // System (Digital)
    System: {
        CloudMusic: 101 // Toggle
    },
    // LED Wall (Analog)
    LED: {
        Brightness: 2
    },
    // Serial Strings
    Serial: {
        DisplayTitle: 1
    },
    // Admin (Digital)
    Admin: {
        AutoOn: {
            '5am': 110, '6am': 111, '7am': 112, '8am': 113, '9am': 114, '10am': 115, 'disable': 116
        },
        AutoOff: {
            '4pm': 120, '5pm': 121, '6pm': 122, '7pm': 123, '8pm': 124, '9pm': 125, 'disable': 126
        }
    }
};

// --- DEBUG LOGGER ---
function logToScreen(msg) {
    // Simplified for production/cleanliness
    console.log(msg);
}

document.addEventListener('DOMContentLoaded', () => {
    logToScreen("DOM Loaded. Initializing...");
    
    initApp();
});

function initApp() {
    logToScreen("Initializing App...");
    
    if (window.CrComLib) {
        logToScreen(`CrComLib Version: ${window.CrComLib.version || 'Unknown'}`);
        if (window.CrComLib.isCrestronDevice && window.CrComLib.isCrestronDevice()) {
             logToScreen("Environment: Native Crestron Device (Detected)");
        } else {
             logToScreen("Environment: Non-Native (Browser/WebXPanel?)");
        }
    } else {
        logToScreen("CRITICAL ERROR: CrComLib not loaded!");
    }

    setupCrComLib();
    setupNavigation();
    setupRouting();
    setupVolume();
    setupSerial();
    setupModals();
    setupDeviceControls();
    
    // Request Initial State from Processor if needed (often handled by subscription update on connect)
}

function setupCrComLib() {
    // Ensure Library is available
    if (!window.CrComLib) {
        logToScreen("ERROR: CrComLib not found on window!");
        return;
    }
    logToScreen("CrComLib found. Subscribing to signals...");
    
    // Subscribe to Source Feedback (Exclusive Feedback expected from Processor)
    // We loop through our map to subscribe
    Object.keys(JOINS.Source).forEach(key => {
        const id = JOINS.Source[key];
        window.CrComLib.subscribeState('b', id.toString(), (value) => {
            if (value) {
                updateSourceUI(key.toLowerCase());
            }
        });
    });

    // Subscribe to Volume Feedback
    window.CrComLib.subscribeState('n', JOINS.Volume.MasterLevel.toString(), (value) => {
        // Convert 16-bit (0-65535) to % (0-100)
        const pct = Math.round((value / 65535) * 100);
        updateVolumeUI(pct);
    });

    window.CrComLib.subscribeState('b', JOINS.Volume.MasterMute.toString(), (value) => {
        updateMuteUI(value);
    });

    // Subscribe to Cloud Music Status
    window.CrComLib.subscribeState('b', JOINS.System.CloudMusic.toString(), (value) => {
        updateCloudMusicUI(value);
    });

    // Subscribe to LED Brightness
    window.CrComLib.subscribeState('n', JOINS.LED.Brightness.toString(), (value) => {
        // value is 0-65535. Convert to 0-100
        const pct = Math.round((value / 65535) * 100);
        
        // Find matching button (approximate)
        const buttons = document.querySelectorAll('#modal-led .list-btn');
        buttons.forEach(btn => {
            const btnVal = parseInt(btn.dataset.val);
            // Allow small margin of error for rounding
            if (Math.abs(btnVal - pct) < 5) {
                btn.classList.add('active');
            } else {
                btn.classList.remove('active');
            }
        });
    });

    // Subscribe to Camera Preset Saved Feedback
    window.CrComLib.subscribeState('b', JOINS.Camera.PresetSaved.toString(), (value) => {
        const savedSpan = document.querySelector('.status-saved');
        if (savedSpan) {
            if (value) {
                savedSpan.classList.add('visible');
            } else {
                savedSpan.classList.remove('visible');
            }
        }
    });

    // Subscribe to Admin Auto On Feedback
    Object.keys(JOINS.Admin.AutoOn).forEach(key => {
        const id = JOINS.Admin.AutoOn[key];
        window.CrComLib.subscribeState('b', id.toString(), (value) => {
            const btn = document.querySelector(`.admin-time-btn[data-type="on"][data-val="${key}"]`);
            if (btn) {
                if (value) btn.classList.add('active');
                else btn.classList.remove('active');
            }
        });
    });

    // Subscribe to Admin Auto Off Feedback
    Object.keys(JOINS.Admin.AutoOff).forEach(key => {
        const id = JOINS.Admin.AutoOff[key];
        window.CrComLib.subscribeState('b', id.toString(), (value) => {
            const btn = document.querySelector(`.admin-time-btn[data-type="off"][data-val="${key}"]`);
            if (btn) {
                if (value) btn.classList.add('active');
                else btn.classList.remove('active');
            }
        });
    });
}

function setupSerial() {
    if (!window.CrComLib) return;

    window.CrComLib.subscribeState('s', JOINS.Serial.DisplayTitle.toString(), (value) => {
        const label = document.getElementById('display-title-label');
        if (label) {
            label.textContent = value;
        }
    });
}

// --- UI UPDATE FUNCTIONS (Reacting to Feedback) ---

function updateSourceUI(sourceKey) {
    // Convert key back to DOM ID format if necessary (e.g., 'cable1' matches)
    const sources = document.querySelectorAll('.source-btn');
    const displayContent = document.getElementById('main-display-content');

    // Logic to map 'wallplate' key to 'wallplate' data-id
    const idMap = {
        'off': 'off',
        'wallplate': 'wallplate',
        'cable1': 'cable1',
        'cable2': 'cable2',
        'appspace': 'appspace',
        'clickshare': 'clickshare'
    };
    
    const targetId = idMap[sourceKey];
    if (!targetId) return;

    sources.forEach(btn => {
        btn.classList.remove('active');
        if (btn.dataset.id === targetId) {
            btn.classList.add('active');
            
            // Update Text Display
            const iconImg = btn.querySelector('.icon-img'); // Check for img first
            const iconDiv = btn.querySelector('.icon');     // Fallback to div
            
            let iconHtml = '';
            if (iconImg) {
                // Use the image source, scaled up for the main display
                iconHtml = `<img src="${iconImg.src}" style="width: 150px; height: 150px;">`; 
            } else if (iconDiv) {
                // Use the text content (emoji)
                iconHtml = `<div style="font-size:5rem;">${iconDiv.textContent}</div>`;
            }

            const label = btn.querySelector('.label').textContent;
            
            if (displayContent) {
                 displayContent.innerHTML = `
                    <div style="text-align:center; margin-top:50px;">
                        ${iconHtml}
                        <div style="font-size:2rem; margin-top:20px;">${label}</div>
                        <div style="color:#aaa; margin-top:10px;">Active</div>
                    </div>
                `;
            }
            
            // Context Controls
            updateContextControls(targetId);
        }
    });
}

function updateVolumeUI(pct) {
    const slider = document.getElementById('master-vol');
    const text = document.getElementById('master-vol-text');
    const fill = document.querySelector('.vol-fill');
    const container = document.querySelector('.vol-slider-container');

    if (slider) slider.value = pct;
    if (text) text.textContent = `${pct}%`;
    
    if (fill && container) {
        const containerHeight = container.offsetHeight;
        const fillHeight = (pct / 100) * containerHeight;
        fill.style.height = `${fillHeight}px`;
    }
}

function updateMuteUI(isMuted) {
    const muteBtn = document.getElementById('master-mute');
    if (muteBtn) {
        muteBtn.classList.toggle('muted', isMuted);
        muteBtn.textContent = isMuted ? 'Unmute' : 'Mute';
    }
}

function updateCloudMusicUI(isOn) {
    const statusSpan = document.getElementById('cloud-status');
    const cloudBtn = document.getElementById('btn-cloud-cover'); // Get the button itself

    if (statusSpan) {
        statusSpan.textContent = isOn ? 'On' : 'Off';
        statusSpan.style.color = isOn ? '#0f0' : '#ff9999';
    }
    if (cloudBtn) {
        cloudBtn.classList.toggle('on-state', isOn); // Toggle the class on the button
    }
}

function updateContextControls(sourceId) {
    const cableBtn = document.getElementById('btn-cable-controls');
    if (cableBtn) {
        if (sourceId === 'cable1' || sourceId === 'cable2') {
            cableBtn.classList.remove('hidden');
        } else {
            cableBtn.classList.add('hidden');
        }
    }
}


// --- INTERACTION SETUP (Sending Signals) ---

function sendDigital(joinId, value) {
    if (window.CrComLib) {
        try {
            window.CrComLib.publishEvent('b', joinId.toString(), value);
            logToScreen(`Tx Digital [${joinId}]: ${value}`);
        } catch (e) {
            logToScreen(`ERROR sending Digital [${joinId}]: ${e.message}`);
            console.error(e);
        }
    } else {
        logToScreen(`FAIL Digital [${joinId}]: Lib not ready`);
    }
}

function sendAnalog(joinId, value) {
    if (window.CrComLib) {
        try {
             window.CrComLib.publishEvent('n', joinId.toString(), value);
             // logToScreen(`Tx Analog [${joinId}]: ${value}`); // Too verbose for sliders
        } catch (e) {
             logToScreen(`ERROR sending Analog [${joinId}]: ${e.message}`);
        }
    }
}

function pulseDigital(joinId) {
    sendDigital(joinId, true);
    setTimeout(() => sendDigital(joinId, false), 200); // Standard 'Press and Release'
}

function setupRouting() {
    const sources = document.querySelectorAll('.source-btn');
    sources.forEach(btn => {
        btn.addEventListener('click', () => {
            // Find join ID
            const sourceId = btn.dataset.id;
            let joinId = 0;
            
            if (sourceId === 'off') joinId = JOINS.Source.Off;
            if (sourceId === 'wallplate') joinId = JOINS.Source.Wallplate;
            if (sourceId === 'cable1') joinId = JOINS.Source.Cable1;
            if (sourceId === 'cable2') joinId = JOINS.Source.Cable2;
            if (sourceId === 'appspace') joinId = JOINS.Source.Appspace;
            if (sourceId === 'clickshare') joinId = JOINS.Source.Clickshare;

            if (joinId > 0) {
                // We pulse the source selection (interlock logic usually on processor)
                pulseDigital(joinId);
            }
        });
    });
}

function setupVolume() {
    const slider = document.getElementById('master-vol');
    const muteBtn = document.getElementById('master-mute');

    if (slider) {
        // Send analog value on change
        // For smoother dragging, we might want to throttle this, but for now direct binding:
        slider.addEventListener('input', (e) => {
            const pct = parseInt(e.target.value);
            // Convert to 16-bit
            const val16 = Math.round((pct / 100) * 65535);
            sendAnalog(JOINS.Volume.MasterLevel, val16);
        });
    }

    if (muteBtn) {
        muteBtn.addEventListener('click', () => {
            pulseDigital(JOINS.Volume.MasterMute);
        });
    }
}

function setupNavigation() {
     // Cloud Cover
    const cloudBtn = document.getElementById('btn-cloud-cover');
    if (cloudBtn) {
        cloudBtn.addEventListener('click', () => {
            pulseDigital(JOINS.System.CloudMusic);
        });
    }

    // LED Brightness Trigger (Just opens modal)
    const ledBtn = document.getElementById('btn-led-brightness');
    if (ledBtn) {
        ledBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            toggleModal('modal-led');
        });
    }

    const logoBtn = document.getElementById('taurus-logo'); // Changed to Taurus logo
    if (logoBtn) {
        logoBtn.addEventListener('click', () => {
            showModal('modal-admin');
        });
    }
}

function setupDeviceControls() {
    // LED Brightness (Analog Send)
    document.querySelectorAll('#modal-led .list-btn').forEach(btn => {
        const handleLedPress = (e) => {
            if (e.type === 'touchstart') {
                e.preventDefault(); // Prevent ghost click & long-press behaviors
            }
            
            // Ensure we get the button element even if a child was clicked
            const targetBtn = e.currentTarget; 
            const val = parseInt(targetBtn.dataset.val); 
            
            // Scale to 0-65535
            const val16 = Math.round((val / 100) * 65535);
            sendAnalog(JOINS.LED.Brightness, val16);
            closeModals();
        };

        btn.addEventListener('click', handleLedPress);
        btn.addEventListener('touchstart', handleLedPress);
    });

    // Admin Controls (Digital Send)
    document.querySelectorAll('#modal-admin .admin-time-btn').forEach(btn => {
        btn.addEventListener('click', () => {
             const type = btn.dataset.type; // 'on' or 'off'
             const valRaw = btn.dataset.val; // '5am', '4pm', 'disable'
             
             let joinId = 0;
             if (type === 'on' && JOINS.Admin.AutoOn[valRaw]) {
                 joinId = JOINS.Admin.AutoOn[valRaw];
             } else if (type === 'off' && JOINS.Admin.AutoOff[valRaw]) {
                 joinId = JOINS.Admin.AutoOff[valRaw];
             }

             if (joinId > 0) {
                 pulseDigital(joinId);
             }
        });
    });

    // Camera Select
    document.querySelectorAll('.cam-sel-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            const id = btn.dataset.id;
            // Update Local UI instantly for responsiveness, or wait for feedback
            // Ideally wait for feedback, but we can do optimistic update:
            document.querySelectorAll('.cam-sel-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');

            if (id === 'podium') pulseDigital(JOINS.Camera.SelectPodium);
            if (id === 'room') pulseDigital(JOINS.Camera.SelectRoom);
        });
    });

    // Camera PTZ (Press/Release logic for movement)
    const camBtns = document.querySelectorAll('#modal-camera .dpad-btn, #modal-camera .zoom-btn');
    camBtns.forEach(btn => {
        const cmd = btn.dataset.cmd;
        let joinId = 0;
        
        if (cmd === 'tilt_up') joinId = JOINS.Camera.TiltUp;
        if (cmd === 'tilt_down') joinId = JOINS.Camera.TiltDown;
        if (cmd === 'pan_left') joinId = JOINS.Camera.PanLeft;
        if (cmd === 'pan_right') joinId = JOINS.Camera.PanRight;
        if (cmd === 'zoom_in') joinId = JOINS.Camera.ZoomIn;
        if (cmd === 'zoom_out') joinId = JOINS.Camera.ZoomOut;

        if (joinId > 0) {
            // PTZ usually requires "Hold to move" logic
            // MouseDown/TouchStart = Signal High
            // MouseUp/TouchEnd = Signal Low
            
            const startHandler = (e) => {
                e.preventDefault();
                sendDigital(joinId, true);
            };
            const stopHandler = (e) => {
                e.preventDefault();
                sendDigital(joinId, false);
            };

            btn.addEventListener('mousedown', startHandler);
            btn.addEventListener('mouseup', stopHandler);
            btn.addEventListener('mouseleave', stopHandler);
            
            btn.addEventListener('touchstart', startHandler);
            btn.addEventListener('touchend', stopHandler);
            btn.addEventListener('touchcancel', stopHandler);
        }
    });

    // Camera Presets
    document.querySelectorAll('.preset-btn').forEach(btn => {
        const id = parseInt(btn.dataset.id);
        // Map 1-5 to 31-35
        const joinId = 30 + id; 

        const startHandler = (e) => {
            if (e.type === 'touchstart') e.preventDefault();
            sendDigital(joinId, true);
        };
        const stopHandler = (e) => {
            if (e.type === 'touchend' || e.type === 'touchcancel') e.preventDefault();
            sendDigital(joinId, false);
        };

        // Mouse Events
        btn.addEventListener('mousedown', startHandler);
        btn.addEventListener('mouseup', stopHandler);
        btn.addEventListener('mouseleave', stopHandler);

        // Touch Events
        btn.addEventListener('touchstart', startHandler);
        btn.addEventListener('touchend', stopHandler);
        btn.addEventListener('touchcancel', stopHandler);
    });

    // Cable Box Controls
    // Simple Pulse Logic
    const cableMap = {
        'up': JOINS.Cable.Up, 'down': JOINS.Cable.Down, 'left': JOINS.Cable.Left, 'right': JOINS.Cable.Right, 'ok': JOINS.Cable.OK,
        'menu': JOINS.Cable.Menu, 'guide': JOINS.Cable.Guide, 'info': JOINS.Cable.Info, 'exit': JOINS.Cable.Exit,
        'ch_up': JOINS.Cable.ChUp, 'ch_down': JOINS.Cable.ChDown,
        'prev_channel': JOINS.Cable.PrevChannel
    };

    document.querySelectorAll('#modal-cable button').forEach(btn => {
        btn.addEventListener('click', (e) => {
            if (btn.classList.contains('close-btn')) return;
            
            const cmd = btn.dataset.cmd;
            let joinId = 0;

            // Check map
            if (cableMap[cmd]) joinId = cableMap[cmd];
            
            // Check numbers
            if (!isNaN(parseInt(cmd))) {
                // 0-9
                const num = parseInt(cmd);
                if (num === 0) joinId = JOINS.Cable.Num0;
                else joinId = 50 + num; // 1->51, etc.
            }

            if (joinId > 0) {
                pulseDigital(joinId);
            }
        });
    });

    // Persistent Buttons Triggers
    const camBtn = document.getElementById('btn-cam-controls');
    if (camBtn) camBtn.addEventListener('click', () => showModal('modal-camera'));

    const cableBtn = document.getElementById('btn-cable-controls');
    if (cableBtn) cableBtn.addEventListener('click', () => showModal('modal-cable'));
}


// --- GENERIC MODAL LOGIC (Preserved from original) ---
function setupModals() {
    const closeBtns = document.querySelectorAll('.close-btn');
    closeBtns.forEach(btn => btn.addEventListener('click', closeModals));
}

function showModal(modalId) {
    const container = document.getElementById('modal-container');
    const targetModal = document.getElementById(modalId);
    
    if (!container || !targetModal) return;

    document.querySelectorAll('.modal-content').forEach(m => m.classList.remove('active'));
    targetModal.classList.add('active');
    
    container.classList.remove('hidden');
    container.classList.add('active');
}

function closeModals() {
    const container = document.getElementById('modal-container');
    if (container) {
        container.classList.add('hidden');
        container.classList.remove('active');
    }
}

function toggleModal(modalId) {
    const container = document.getElementById('modal-container');
    const modal = document.getElementById(modalId);
    if (container && modal && container.classList.contains('active') && modal.classList.contains('active')) {
        closeModals();
    } else {
        showModal(modalId);
    }
}