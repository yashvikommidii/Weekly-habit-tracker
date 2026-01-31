const API_BASE = '/api/habits';

let habits = [];
let entries = {};
let habitChart = null;
let selectedDate = new Date();
let graphMode = 'weekly';

const HABIT_QUOTES = [
    "Tracking habits isn't about perfection—it's about progress. Every checkmark is a win.",
    "The habit of tracking creates accountability. What gets measured gets improved.",
    "Small daily improvements lead to stunning long-term results. Keep tracking!",
    "Your future is created by what you do today. Track it, own it.",
    "Consistency beats intensity. One day at a time.",
    "Don't break the chain! Every day you track is a day you care.",
    "Habits are the compound interest of self-improvement. Track yours.",
    "The secret of getting ahead is getting started—and tracking your progress.",
    "You don't have to be great to start, but you have to start to be great.",
    "Track your habits. Celebrate your wins. Repeat."
];

function getWeekStart(d) {
    const copy = new Date(d);
    copy.setDate(copy.getDate() - copy.getDay());
    return copy;
}

function getDateStr(d) {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
}

function getWeekDates() {
    const start = getWeekStart(selectedDate);
    const dates = [];
    for (let i = 0; i < 7; i++) {
        const d = new Date(start);
        d.setDate(start.getDate() + i);
        dates.push(d);
    }
    return dates;
}

function initDatePicker() {
    const input = document.getElementById('weekDate');
    const today = new Date();
    input.value = getDateStr(today);
    selectedDate = new Date(today);
    input.addEventListener('change', () => {
        if (input.value) selectedDate = new Date(input.value + 'T12:00:00');
        loadAndRender();
    });
}

async function fetchHabits() {
    const res = await fetch(API_BASE);
    return res.ok ? res.json() : [];
}

async function addHabit(name) {
    const res = await fetch(API_BASE, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name })
    });
    if (!res.ok) throw new Error('Failed to add habit');
    return res.json();
}

async function logEntry(habitId, dateStr, completed) {
    const res = await fetch(`${API_BASE}/entries`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ habitId, date: dateStr, completed })
    });
    if (!res.ok) throw new Error('Failed to log entry');
    return res.json();
}

const QUOTES_API = '/api/MotivationalQuotes';

async function fetchMotivationalQuotes() {
    const res = await fetch(QUOTES_API);
    return res.ok ? res.json() : [];
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

async function fetchGraphData() {
    const params = new URLSearchParams({ cache: 'no-store' });
    if (graphMode === 'weekly') {
        const start = getWeekStart(selectedDate);
        params.set('weekStart', getDateStr(start));
        params.set('mode', 'weekly');
    } else {
        const monthStart = `${selectedDate.getFullYear()}-${String(selectedDate.getMonth() + 1).padStart(2, '0')}-01`;
        params.set('monthStart', monthStart);
        params.set('mode', 'monthly');
    }
    const res = await fetch(`${API_BASE}/graph?${params}`, { cache: 'no-store', headers: { 'Cache-Control': 'no-cache' } });
    return res.ok ? res.json() : [];
}

function getEntry(habitId, dateStr) {
    return entries[habitId]?.[dateStr];
}

function setEntry(habitId, dateStr, completed) {
    if (!entries[habitId]) entries[habitId] = {};
    entries[habitId][dateStr] = completed;
}

function renderHabits() {
    const list = document.getElementById('habitsList');
    if (habits.length === 0) {
        list.innerHTML = '<p class="empty-state">No habits yet. Add one above!</p>';
        return;
    }
    list.innerHTML = habits.map(h =>
        `<div class="habit-bubble" title="${h.name}">${h.name}</div>`
    ).join('');
}

function renderTracker() {
    const grid = document.getElementById('trackerGrid');
    const weekDates = getWeekDates();
    const dayNames = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

    if (habits.length === 0) {
        grid.innerHTML = '<p class="empty-state">Add habits first to start tracking.</p>';
        return;
    }

    let html = '<table class="tracker-table"><thead><tr><th>Habit</th>';
    weekDates.forEach((d, i) => {
        html += `<th>${dayNames[i]}<br><span class="date-sub">${d.getMonth() + 1}/${d.getDate()}</span></th>`;
    });
    html += '<th class="total-col">Total</th></tr></thead><tbody>';

    habits.forEach(habit => {
        let doneCount = 0;
        html += `<tr><td>${habit.name}</td>`;
        weekDates.forEach(d => {
            const dateStr = getDateStr(d);
            const entry = getEntry(habit.id, dateStr);
            if (entry === true) doneCount++;
            let cls = 'tracker-cell';
            let symbol = '—';
            if (entry === true) { cls += ' did'; symbol = '✓'; }
            else if (entry === false) { cls += ' didnt'; symbol = '✗'; }
            html += `<td><div class="${cls}" data-habit-id="${habit.id}" data-date="${dateStr}" title="Click to toggle">${symbol}</div></td>`;
        });
        html += `<td class="total-cell">${doneCount}/7</td></tr>`;
    });
    html += '</tbody></table>';
    grid.innerHTML = html;

    grid.querySelectorAll('.tracker-cell').forEach(cell => {
        cell.addEventListener('click', async () => {
            const habitId = parseInt(cell.dataset.habitId);
            const dateStr = cell.dataset.date;
            const current = getEntry(habitId, dateStr);
            let next = true;
            if (current === true) next = false;
            else if (current === false) next = true;
            setEntry(habitId, dateStr, next);
            try {
                await logEntry(habitId, dateStr, next);
                renderTracker();
                await renderGraph();
            } catch (e) {
                setEntry(habitId, dateStr, current);
                renderTracker();
                alert('Could not save. Is the API running?');
            }
        });
    });
}

async function renderGraph() {
    const canvas = document.getElementById('habitChart');
    const emptyMsg = document.getElementById('graphEmpty');

    const data = await fetchGraphData();
    if (!data || data.length === 0 || habits.length === 0) {
        canvas.style.display = 'none';
        emptyMsg.style.display = 'block';
        emptyMsg.textContent = graphMode === 'monthly' ? 'Add habits and log entries to see monthly progress.' : 'Add habits and log entries to see your graph.';
        if (habitChart) {
            habitChart.destroy();
            habitChart = null;
        }
        return;
    }

    const labels = data.map(d => d.day);
    const percentages = data.map(d =>
        d.totalHabits > 0 ? Math.round((d.completedCount / d.totalHabits) * 100) : 0
    );

    canvas.style.display = 'block';
    emptyMsg.style.display = 'none';

    const isMonthly = graphMode === 'monthly' && data.length > 7;
    const scrollWrap = document.querySelector('.graph-scroll-wrap');
    if (scrollWrap) {
        scrollWrap.style.overflowX = isMonthly ? 'auto' : 'visible';
        scrollWrap.style.width = isMonthly ? `${Math.max(900, data.length * 30)}px` : '100%';
    }

    if (habitChart) habitChart.destroy();

    const ctx = canvas.getContext('2d');
    Chart.defaults.color = '#a8a29e';
    habitChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: 'Completion %',
                data: percentages,
                borderColor: '#84cc16',
                backgroundColor: 'rgba(132, 204, 22, 0.1)',
                borderWidth: 2,
                fill: false,
                pointBackgroundColor: '#a3e635',
                pointBorderColor: '#84cc16',
                pointRadius: 5,
                pointHoverRadius: 7,
                tension: 0.2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false }
            },
                scales: {
                    y: {
                        beginAtZero: true,
                        max: 100,
                        ticks: {
                            callback: value => value + '%',
                            stepSize: 10
                        },
                        grid: { display: false },
                        border: { display: false }
                    },
                    x: {
                        grid: { display: false },
                        border: { display: false },
                        ticks: {
                            maxTicksLimit: graphMode === 'monthly' ? 31 : 7,
                            maxRotation: 45
                        }
                    }
                }
        }
    });
}

async function loadAndRender() {
    try {
        habits = await fetchHabits();
    } catch {
        document.getElementById('habitsList').innerHTML =
            '<p class="empty-state">Could not connect to the API. Make sure the server is running (dotnet run).</p>';
        document.getElementById('trackerGrid').innerHTML = '';
        document.getElementById('habitChart').style.display = 'none';
        document.getElementById('graphEmpty').style.display = 'block';
        document.getElementById('graphEmpty').textContent = 'Start the server to see your graph.';
        if (habitChart) { habitChart.destroy(); habitChart = null; }
        return;
    }
    entries = {};
    const weekDates = getWeekDates();
    const fromDate = getDateStr(weekDates[0]);
    const toDate = getDateStr(weekDates[6]);
    for (const h of habits) {
        try {
            const res = await fetch(`${API_BASE}/${h.id}/entries?fromDate=${fromDate}&toDate=${toDate}`);
            const list = res.ok ? await res.json() : [];
            entries[h.id] = {};
            list.forEach(e => { entries[h.id][e.date] = e.completed; });
        } catch {
            entries[h.id] = {};
        }
    }
    renderHabits();
    renderTracker();
    await renderGraph();
    await loadAwards();
}

document.getElementById('addHabitBtn').addEventListener('click', async () => {
    const input = document.getElementById('habitName');
    const name = input.value.trim();
    if (!name) return;
    try {
        await addHabit(name);
        input.value = '';
        await loadAndRender();
    } catch (e) {
        alert('Could not add habit. Is the API running?');
    }
});

document.getElementById('habitName').addEventListener('keypress', e => {
    if (e.key === 'Enter') document.getElementById('addHabitBtn').click();
});

document.getElementById('whatsUpBtn').addEventListener('click', async () => {
    const output = document.getElementById('whatsUpOutput');
    try {
        const quotes = await fetchMotivationalQuotes();
        const allQuotes = quotes.length > 0
            ? quotes.map(q => ({ text: q.quote, author: q.author }))
            : HABIT_QUOTES.map(t => ({ text: t, author: '' }));
        const pick = allQuotes[Math.floor(Math.random() * allQuotes.length)];
        output.innerHTML = `<p class="quote-text">"${escapeHtml(pick.text)}"</p>${pick.author ? `<p class="quote-author">— ${escapeHtml(pick.author)}</p>` : ''}`;
        output.style.display = 'block';
    } catch (e) {
        const pick = HABIT_QUOTES[Math.floor(Math.random() * HABIT_QUOTES.length)];
        output.innerHTML = `<p class="quote-text">"${escapeHtml(pick)}"</p>`;
        output.style.display = 'block';
    }
});

async function fetchAwards() {
    const monthStart = `${selectedDate.getFullYear()}-${String(selectedDate.getMonth() + 1).padStart(2, '0')}-01`;
    const res = await fetch(`${API_BASE}/awards?monthStart=${encodeURIComponent(monthStart)}`);
    if (!res.ok) return {};
    return res.json();
}

async function loadAwards() {
    const container = document.getElementById('awardsContainer');
    if (!container) return;
    try {
        const awards = await fetchAwards();
        renderAwards(awards);
    } catch (e) {
        container.innerHTML = '<p class="empty-state">Could not load awards. Add habits and track entries to see them.</p>';
    }
}

function renderAwards(awards) {
    const container = document.getElementById('awardsContainer');
    const t = awards.topActivity;
    const l = awards.lowestActivity;
    const s = awards.highestStreakActivity;
    if (!t && !l && !s) {
        container.innerHTML = '<p class="empty-state">Add habits and log entries to earn awards!</p>';
        return;
    }
    let html = '';
    if (t) html += `<div class="award-card award-top"><span class="award-icon">🏆</span><h4>Top Performer</h4><p>${escapeHtml(t.name)}</p><span class="award-count">${t.count} completed</span></div>`;
    if (l) html += `<div class="award-card award-low"><span class="award-icon">🌱</span><h4>Needs a Boost</h4><p>${escapeHtml(l.name)}</p><span class="award-count">${l.count} completed</span></div>`;
    if (s) html += `<div class="award-card award-streak"><span class="award-icon">🔥</span><h4>Longest Streak</h4><p>${escapeHtml(s.name)}</p><span class="award-count">${s.count} days</span></div>`;
    container.innerHTML = html;
}

function initGraphModeToggle() {
    document.querySelectorAll('.graph-mode-btn').forEach(btn => {
        btn.addEventListener('click', async () => {
            graphMode = btn.dataset.mode || 'weekly';
            document.querySelectorAll('.graph-mode-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            await renderGraph();
        });
    });
}

let chatHistory = [];

async function sendChatMessage() {
    const input = document.getElementById('chatInput');
    const log = document.getElementById('chatLog');
    const submitBtn = document.getElementById('chatSubmitBtn');
    const loading = document.getElementById('chatLoading');
    const message = input.value.trim();
    if (!message) return;

    const userDiv = document.createElement('div');
    userDiv.className = 'chat-msg chat-user';
    userDiv.textContent = message;
    log.appendChild(userDiv);
    log.scrollTop = log.scrollHeight;
    input.value = '';
    submitBtn.disabled = true;
    loading.style.display = 'block';

    chatHistory.push({ role: 'user', content: message });

    try {
        const res = await fetch('/api/chat', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ message, history: chatHistory.slice(-10) })
        });
        let data = {};
        try { data = await res.json(); } catch { }
        const reply = data.reply || (res.ok ? 'Could not get a response.' : 'The assistant is unavailable. Please check your API key.');

        chatHistory.push({ role: 'assistant', content: reply });

        const botDiv = document.createElement('div');
        botDiv.className = 'chat-msg chat-bot';
        botDiv.textContent = reply;
        log.appendChild(botDiv);
        log.scrollTop = log.scrollHeight;
    } catch (e) {
        const errDiv = document.createElement('div');
        errDiv.className = 'chat-msg chat-bot chat-error';
        errDiv.textContent = 'Could not reach the assistant. Please try again.';
        log.appendChild(errDiv);
        log.scrollTop = log.scrollHeight;
    } finally {
        submitBtn.disabled = false;
        loading.style.display = 'none';
    }
}

function initChat() {
    document.getElementById('chatSubmitBtn').addEventListener('click', sendChatMessage);
    document.getElementById('chatInput').addEventListener('keydown', e => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendChatMessage();
        }
    });
}

initDatePicker();
initGraphModeToggle();
initChat();
loadAndRender();
