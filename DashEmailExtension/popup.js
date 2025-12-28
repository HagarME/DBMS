document.getElementById('saveBtn').addEventListener('click', async () => {
    const statusDiv = document.getElementById('status');
    statusDiv.textContent = "Processing...";
    statusDiv.className = "status";

    const tabs = await chrome.tabs.query({ active: true, currentWindow: true });
    if (!tabs || !tabs[0]) {
        statusDiv.textContent = "Error: No active tab.";
        statusDiv.className = "status error";
        return;
    }
    const tabId = tabs[0].id;

    try {
        // Step 1: Get IK Token from MAIN world (Gmail's global scope)
        let ik = null;
        try {
            const results = await chrome.scripting.executeScript({
                target: { tabId: tabId },
                world: 'MAIN',
                func: () => {
                    try {
                        // 1. Check GLOBALS
                        if (window.GLOBALS && Array.isArray(window.GLOBALS)) {
                            for (let item of window.GLOBALS) {
                                if (typeof item === 'string' && /^[a-f0-9]{10}$/.test(item)) return item;
                            }
                        }
                        // 2. Check for 'ik' in scripts
                        const scripts = document.querySelectorAll('script');
                        for (let s of scripts) {
                            const match = s.textContent.match(/"ik"\s*:\s*"([a-f0-9]{10})"/);
                            if (match) return match[1];
                        }
                        // 3. Check Print/NewWindow links
                        const links = document.querySelectorAll('a[href*="ik="]');
                        for (let l of links) {
                            const match = l.href.match(/[?&]ik=([a-f0-9]{10})/);
                            if (match) return match[1];
                        }
                        return null;
                    } catch (e) { return null; }
                }
            });
            if (results && results[0] && results[0].result) {
                ik = results[0].result;
            }
        } catch (e) {
            console.log("Main world script failed", e);
        }

        // Step 2: Fetch Email using that IK
        const results = await chrome.scripting.executeScript({
            target: { tabId: tabId },
            func: fetchSelectedEmailRaw,
            args: [ik]
        });

        if (chrome.runtime.lastError || !results || !results[0] || !results[0].result) {
            statusDiv.textContent = "Error: Communication failure. Refresh Gmail.";
            statusDiv.className = "status error";
            return;
        }

        const rawContent = results[0].result;
        if (rawContent.error) {
            statusDiv.textContent = rawContent.error;
            statusDiv.className = "status error";
            return;
        }

        statusDiv.textContent = "Uploading to DASH...";

        const port = 5004;
        const response = await fetch(`http://localhost:${port}/api/email/ingest-raw`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Content: rawContent })
        });

        if (response.ok) {
            statusDiv.textContent = "Success! Email saved.";
            statusDiv.className = "status success";
            setTimeout(() => window.close(), 2000);
        } else {
            const errText = await response.text();
            statusDiv.textContent = "Server Error: " + errText;
            statusDiv.className = "status error";
        }

    } catch (err) {
        statusDiv.textContent = "Connection Failed! Is DASH running on port 5004?";
        statusDiv.className = "status error";
        console.error(err);
    }
});

document.addEventListener('DOMContentLoaded', () => {
    // UI Cleanup
    const fieldsToHide = ['sender', 'subject', 'body'];
    fieldsToHide.forEach(id => {
        const el = document.getElementById(id);
        if (el) el.style.display = 'none';
    });
    document.querySelectorAll('label').forEach(l => l.style.display = 'none');
    document.getElementById('saveBtn').textContent = "Import Selected Email";
});

async function fetchSelectedEmailRaw(ikFromMain) {
    try {
        let ik = ikFromMain;

        // Final fallback for IK in ISOLATED world (Regex on body)
        if (!ik) {
            const match = document.body.innerHTML.match(/"ik"\s*:\s*"([a-f0-9]{10})"/);
            if (match) ik = match[1];
        }

        // 1. Find the selected row
        const selectedCheckbox = document.querySelector('div[role="checkbox"][aria-checked="true"]');

        if (!selectedCheckbox) {
            const urlParams = new URLSearchParams(window.location.search);
            let threadId = urlParams.get('th');

            if (!threadId) {
                const printBtn = document.querySelector('a[href*="view=pt"]');
                if (printBtn) {
                    const match = printBtn.href.match(/th=([a-f0-9]+)/);
                    if (match) threadId = match[1];
                }
            }

            if (!threadId) {
                return { error: "Please select an email by checking its box." };
            }

            return await downloadEml(threadId, ik);
        }

        // We are in LIST view
        let row = selectedCheckbox.closest('tr') || selectedCheckbox.closest('.zA');
        if (!row) return { error: "Could not identify email row." };

        // Find THREAD ID
        let threadId = null;
        const legacyEl = row.querySelector('[data-legacy-thread-id]');
        if (legacyEl) {
            threadId = legacyEl.getAttribute('data-legacy-thread-id');
        } else {
            const threadSpan = row.querySelector('[data-thread-id]');
            if (threadSpan) {
                const rawId = threadSpan.getAttribute('data-thread-id');
                if (rawId && rawId.startsWith("#thread-f:")) {
                    const decimalId = rawId.split(":")[1];
                    try {
                        threadId = BigInt(decimalId).toString(16);
                    } catch (e) { }
                }
            }
        }

        if (!threadId) return { error: "ID missing. Try opening the email." };

        return await downloadEml(threadId, ik);

    } catch (e) {
        return { error: e.toString() };
    }

    async function downloadEml(thId, ikVal) {
        try {
            // view=om with export=download is the most direct way to get the EML stream
            let omUrl = `https://mail.google.com/mail/u/0/?view=om&th=${thId}`;
            if (ikVal) omUrl += `&ik=${ikVal}`;
            omUrl += `&export=download`;

            const response = await fetch(omUrl);
            const text = await response.text();

            if (text.includes("Temporary Error") || text.includes("<title>Error</title>")) {
                return { error: "Gmail blocked access (404). Try opening the email first." };
            }

            // If we still get a full HTML page, we might be inside the 'Preview' wrapper.
            // Raw EML starts with a Date or Delivered-To header, not a tag.
            if (text.trim().startsWith("<") || text.includes("<!DOCTYPE")) {
                // Final attempt: If it's HTML, check if the raw text is inside a <pre> tag
                const parser = new DOMParser();
                const doc = parser.parseFromString(text, 'text/html');
                const pre = doc.querySelector('pre');
                if (pre && pre.textContent.length > 500) {
                    return pre.textContent;
                }

                const part = text.substring(0, 100).replace(/[<>]/g, "");
                return { error: "Gmail returned HTML wrapper. Snippet: " + part };
            }

            if (text.length < 500) {
                return { error: "Email content too short to be valid." };
            }
            return text;
        } catch (err) {
            return { error: "Fetch failed: " + err.toString() };
        }
    }
}
