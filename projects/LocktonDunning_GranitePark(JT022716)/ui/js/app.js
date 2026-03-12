/**
 * Lockton Dunning Benefits - Break Room Control
 * Mapped for CH5 (Crestron HTML5) on TSW-1070
 */

// --- JOIN MAP CONFIGURATION (Fresh Start) ---
const JOINS = {
    // System
    System: {
        LogicReady: 24,
        PowerOff: 7,
        SettingsModal: 12
    },
    // Source Selection (Digital)
    Source: {
        AirMedia: 5,
        MediaPlayer: 6
    },
    // Volume (Analog: 0-65535, Digital: Mute Toggle)
    Volume: {
        MasterLevel: 1,
        MasterMute: 1,
        Restroom9FLevel: 2,
        Restroom9FMute: 2,
        Restroom10FLevel: 3,
        Restroom10FMute: 3
    },
    // Microphones (Analog Level, Digital Mute)
    Mic: {
        HandheldLevel: 9,
        HandheldMute: 9,
        BodypackLevel: 10,
        BodypackMute: 10
    },
    // Sonos Breakroom
    Sonos: {
        Prev: 14,
        PlayPause: 15,
        Next: 16,
        Volume: 17,
        Metadata: {
            Title: 14,
            Artist: 15,
            Album: 16,
            ArtUrl: 17
        }
    },
    // Sonos Restroom
    SonosRestroom: {
        Prev: 19,
        PlayPause: 20,
        Next: 21,
        Volume: 22,
        Metadata: {
            Title: 19,
            Artist: 20,
            Album: 21,
            ArtUrl: 22
        }
    },
    // Scheduling
    Schedule: {
        On: { '7am': 26, '8am': 27, '9am': 28, 'disable': 29 },
        Off: { '5pm': 31, '6pm': 32, '7pm': 33, 'disable': 34 }
    }
};

// --- INITIALIZATION ---
document.addEventListener('DOMContentLoaded', () => {
    console.log("Lockton Dunning App Loaded");

    startClock();
    initApp();
});

function initApp() {
    setupCrComLib();
    setupNavigation();
    setupVolume();
    setupSettings();

    // Simulate initial state for testing if not connected
    if (!window.CrComLib || !window.CrComLib.isCrestronDevice()) {
        console.log("Dev Mode: Simulating feedback...");
        // updateSourceUI('airmedia'); // Auto-select for dev check

        // Simulate Scheduling Defaults (e.g. 8am - 6pm)
        setTimeout(() => {
            updateSchedUI('on', '8am', true);
            updateSchedUI('off', '6pm', true);
        }, 500);
    }
}

function startClock() {
    const updateTime = () => {
        const now = new Date();

        // Time
        let hours = now.getHours();
        const ampm = hours >= 12 ? 'PM' : 'AM';
        hours = hours % 12;
        hours = hours ? hours : 12;
        const minutes = now.getMinutes().toString().padStart(2, '0');
        document.getElementById('clock-display').textContent = `${hours}:${minutes} ${ampm}`;

        // Date
        const options = { weekday: 'long', month: 'short', day: 'numeric' };
        document.getElementById('date-display').textContent = now.toLocaleDateString('en-US', options);
    };

    updateTime();
    setInterval(updateTime, 10000); // Update every 10s
}

// --- CRESTRON LIBRARY SETUP ---
function setupCrComLib() {
    if (!window.CrComLib) {
        console.warn("CrComLib not found (Browser Mode)");
        return;
    }

    // -- Subscriptions --

    // Logic Ready (Synchronization) - Join 100
    window.CrComLib.subscribeState('b', JOINS.System.LogicReady.toString(), (val) => {
        if (val) {
            console.log("SIMPL Logic Ready. Dismissing overlay.");
            const overlay = document.getElementById('boot-overlay');
            if (overlay) overlay.classList.add('ready');
        }
    });

    // Source Feedback
    Object.keys(JOINS.Source).forEach(key => {
        const id = JOINS.Source[key];
        window.CrComLib.subscribeState('b', id.toString(), (val) => {
            if (val) updateSourceUI(key.toLowerCase());
        });
    });

    // Breakroom Sonos Metadata
    window.CrComLib.subscribeState('s', JOINS.Sonos.Metadata.Title.toString(), (val) => {
        document.getElementById('sonos-br-title').textContent = val || 'Not Playing';
    });
    window.CrComLib.subscribeState('s', JOINS.Sonos.Metadata.Artist.toString(), (val) => {
        document.getElementById('sonos-br-artist').textContent = val || 'Select Music';
    });

    // Restroom Sonos Metadata
    window.CrComLib.subscribeState('s', JOINS.SonosRestroom.Metadata.Title.toString(), (val) => {
        document.getElementById('sonos-rr-title').textContent = val || 'Not Playing';
    });
    window.CrComLib.subscribeState('s', JOINS.SonosRestroom.Metadata.Artist.toString(), (val) => {
        document.getElementById('sonos-rr-artist').textContent = val || 'Sonos BGM';
    });

    // Power Off Feedback (Switch to Empty View)
    window.CrComLib.subscribeState('b', JOINS.System.PowerOff.toString(), (val) => {
        if (val) showView('view-empty');
    });

    // Master Volume
    window.CrComLib.subscribeState('n', JOINS.Volume.MasterLevel.toString(), (val) => {
        updateSliderUI('master-vol', val);
    });
    window.CrComLib.subscribeState('b', JOINS.Volume.MasterMute.toString(), (val) => {
        updateMuteUI('master-mute', val);
    });

    // 9th Floor Restroom Volume
    window.CrComLib.subscribeState('n', JOINS.Volume.Restroom9FLevel.toString(), (val) => {
        updateSliderUI('vol-restroom-9f', val);
    });
    window.CrComLib.subscribeState('b', JOINS.Volume.Restroom9FMute.toString(), (val) => {
        updateMuteUI('mute-restroom-9f', val);
    });

    // 10th Floor Restroom Volume
    window.CrComLib.subscribeState('n', JOINS.Volume.Restroom10FLevel.toString(), (val) => {
        updateSliderUI('vol-restroom-10f', val);
    });
    window.CrComLib.subscribeState('b', JOINS.Volume.Restroom10FMute.toString(), (val) => {
        updateMuteUI('mute-restroom-10f', val);
    });

    // Mic Volumes
    window.CrComLib.subscribeState('n', JOINS.Mic.HandheldLevel.toString(), (val) => {
        updateSliderUI('vol-mic-handheld', val);
    });
    window.CrComLib.subscribeState('b', JOINS.Mic.HandheldMute.toString(), (val) => {
        updateMuteUI('mute-mic-handheld', val);
    });
    window.CrComLib.subscribeState('n', JOINS.Mic.BodypackLevel.toString(), (val) => {
        updateSliderUI('vol-mic-bodypack', val);
    });
    window.CrComLib.subscribeState('b', JOINS.Mic.BodypackMute.toString(), (val) => {
        updateMuteUI('mute-mic-bodypack', val);
    });

    // Scheduling Feedback
    Object.keys(JOINS.Schedule.On).forEach(key => {
        window.CrComLib.subscribeState('b', JOINS.Schedule.On[key].toString(), (val) => {
            updateSchedUI('on', key, val);
        });
    });
    Object.keys(JOINS.Schedule.Off).forEach(key => {
        window.CrComLib.subscribeState('b', JOINS.Schedule.Off[key].toString(), (val) => {
            updateSchedUI('off', key, val);
        });
    });
}

// --- SENDING SIGNALS ---
function sendDigital(id, val) {
    if (window.CrComLib) window.CrComLib.publishEvent('b', id.toString(), val);
    console.log(`Tx Digital [${id}]: ${val}`);
}

function pulseDigital(id) {
    sendDigital(id, true);
    setTimeout(() => sendDigital(id, false), 200);
}

function sendAnalog(id, val) {
    if (window.CrComLib) window.CrComLib.publishEvent('n', id.toString(), val);
    // console.log(`Tx Analog [${id}]: ${val}`); // Verbose
}

// --- UI LOGIC ---

function setupNavigation() {
    // Source Buttons
    document.querySelectorAll('.source-select').forEach(btn => {
        btn.addEventListener('click', () => {
            const id = btn.dataset.id;
            // Map ID to Join
            if (id === 'airmedia') pulseDigital(JOINS.Source.AirMedia);
            if (id === 'mediaplayer') pulseDigital(JOINS.Source.MediaPlayer);

            // Optimistic UI update (wait for feedback in real world, but good for responsiveness)
            updateSourceUI(id);
        });
    });

    // Power Off
    const pwrBtn = document.getElementById('btn-power-off');
    if (pwrBtn) {
        pwrBtn.addEventListener('click', () => {
            pulseDigital(JOINS.System.PowerOff);
            showView('view-empty');
        });
    }
}

function showView(viewId) {
    // Hide all views explicitly
    const allViews = document.querySelectorAll('.stage-card, .source-view, .empty-state');
    allViews.forEach(el => {
        el.classList.remove('active');
    });

    // Show target
    const target = document.getElementById(viewId);
    if (target) {
        target.classList.add('active');
    }

    // Handle Sidebar Active State
    document.querySelectorAll('.nav-btn').forEach(btn => btn.classList.remove('active'));

    if (viewId === 'view-empty') {
        document.getElementById('btn-power-off').classList.add('active');
    } else if (viewId === 'view-airmedia') {
        document.querySelector('.nav-btn[data-id="airmedia"]').classList.add('active');
    } else if (viewId === 'view-mediaplayer') {
        document.querySelector('.nav-btn[data-id="mediaplayer"]').classList.add('active');
    }
}

function updateSourceUI(sourceKey) {
    // sourceKey matches the keys in JOINS.Source (lowercase) OR the data-id in HTML
    if (sourceKey === 'airmedia') showView('view-airmedia');
    if (sourceKey === 'mediaplayer') showView('view-mediaplayer');
}

function setupVolume() {
    // Helper for slider listeners
    const bindSlider = (elId, joinId) => {
        const el = document.getElementById(elId);
        if (!el) return;
        el.addEventListener('input', (e) => {
            const val = parseInt(e.target.value);
            sendAnalog(joinId, val);
            // Local update for immediate feedback
            updateSliderUI(elId, val);
        });
    };

    bindSlider('master-vol', JOINS.Volume.MasterLevel);
    bindSlider('vol-restroom-9f', JOINS.Volume.Restroom9FLevel);
    bindSlider('vol-restroom-10f', JOINS.Volume.Restroom10FLevel);
    bindSlider('vol-mic-handheld', JOINS.Mic.HandheldLevel);
    bindSlider('vol-mic-bodypack', JOINS.Mic.BodypackLevel);

    // Helper for Mute listeners
    const bindMute = (elId, joinId) => {
        const el = document.getElementById(elId);
        if (!el) return;
        el.addEventListener('click', () => {
            pulseDigital(joinId);
            // Toggle local class optimistically? Better to wait for feedback usually.
        });
    };

    bindMute('master-mute', JOINS.Volume.MasterMute);
    bindMute('mute-restroom-9f', JOINS.Volume.Restroom9FMute);
    bindMute('mute-restroom-10f', JOINS.Volume.Restroom10FMute);
    bindMute('mute-mic-handheld', JOINS.Mic.HandheldMute);
    bindMute('mute-mic-bodypack', JOINS.Mic.BodypackMute);
}

function updateSliderUI(elId, val) {
    const input = document.getElementById(elId);
    if (input) input.value = val;

    const pct = Math.round((val / 65535) * 100);

    // Update Horizontal Master Volume Bar
    if (elId === 'master-vol') {
        const fill = document.getElementById('master-vol-fill');
        const text = document.getElementById('master-vol-text');
        if (fill) fill.style.width = `${pct}%`;
        if (text) text.textContent = `${pct}%`;
    }
}

function updateMuteUI(elId, isMuted) {
    const btn = document.getElementById(elId);
    if (!btn) return;

    if (isMuted) {
        btn.classList.add('muted');
        // Handle text change if needed (Master Mute spans)
        const span = btn.querySelector('span');
        if (span) span.textContent = 'Unmute';
        else if (btn.tagName === 'BUTTON') btn.textContent = 'Unmute';
    } else {
        btn.classList.remove('muted');
        const span = btn.querySelector('span');
        if (span) span.textContent = 'Mute';
        else if (btn.tagName === 'BUTTON') btn.textContent = 'Mute'; // Specific for shared setting btns
    }
}

function setupSettings() {
    const modal = document.getElementById('modal-settings');
    const openBtn = document.getElementById('btn-settings');
    const closeBtn = document.getElementById('btn-close-settings');

    openBtn.addEventListener('click', () => {
        modal.classList.add('active');
        pulseDigital(JOINS.System.SettingsModal); // Notify processor (optional)
    });

    closeBtn.addEventListener('click', () => {
        modal.classList.remove('active');
    });

    // Tab Logic
    const tabs = document.querySelectorAll('.settings-tab');
    tabs.forEach(tab => {
        tab.addEventListener('click', () => {
            // Remove active from all tabs
            tabs.forEach(t => t.classList.remove('active'));
            document.querySelectorAll('.tab-pane').forEach(p => p.classList.remove('active'));

            // Activate clicked
            tab.classList.add('active');
            const targetId = `tab-${tab.dataset.tab}`;
            document.getElementById(targetId).classList.add('active');
        });
    });

    // Sonos Breakroom Controls
    document.getElementById('sonos-prev').addEventListener('click', () => pulseDigital(JOINS.Sonos.Prev));
    document.getElementById('sonos-play').addEventListener('click', () => pulseDigital(JOINS.Sonos.PlayPause));
    document.getElementById('sonos-next').addEventListener('click', () => pulseDigital(JOINS.Sonos.Next));

    // Sonos Restroom Controls
    document.getElementById('sonos-restroom-prev').addEventListener('click', () => pulseDigital(JOINS.SonosRestroom.Prev));
    document.getElementById('sonos-restroom-play').addEventListener('click', () => pulseDigital(JOINS.SonosRestroom.PlayPause));
    document.getElementById('sonos-restroom-next').addEventListener('click', () => pulseDigital(JOINS.SonosRestroom.Next));

    // Scheduling Logic
    document.querySelectorAll('#sched-on-container button').forEach(btn => {
        btn.addEventListener('click', () => {
            const val = btn.dataset.val;
            pulseDigital(JOINS.Schedule.On[val]);
        });
    });

    document.querySelectorAll('#sched-off-container button').forEach(btn => {
        btn.addEventListener('click', () => {
            const val = btn.dataset.val;
            pulseDigital(JOINS.Schedule.Off[val]);
        });
    });
}

function updateSchedUI(type, key, active) {
    const containerId = type === 'on' ? 'sched-on-container' : 'sched-off-container';
    const container = document.getElementById(containerId);
    if (!container) return;

    const btn = container.querySelector(`button[data-val="${key}"]`);
    if (btn) {
        if (active) {
            // Remove active from siblings? Feedback usually handles this exclusive logic
            // container.querySelectorAll('button').forEach(b => b.classList.remove('active')); 
            btn.classList.add('active');
        } else {
            btn.classList.remove('active');
        }
    }
}