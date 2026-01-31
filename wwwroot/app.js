const API_BASE = '/api/habits';
const STORAGE_HABITS = 'habitTracker_habits';
const STORAGE_ENTRIES = 'habitTracker_entries';

let habits = [];
let entries = {};
let habitChart = null;
let selectedDate = new Date();
let graphMode = 'weekly';

// === localStorage helpers ===
function loadHabitsFromStorage() {
    try {
        const raw = localStorage.getItem(STORAGE_HABITS);
        return raw ? JSON.parse(raw) : [];
    } catch { return []; }
}

function saveHabitsToStorage(data) {
    localStorage.setItem(STORAGE_HABITS, JSON.stringify(data));
}

function loadEntriesFromStorage() {
    try {
        const raw = localStorage.getItem(STORAGE_ENTRIES);
        return raw ? JSON.parse(raw) : {};
    } catch { return {}; }
}

function saveEntriesToStorage(data) {
    localStorage.setItem(STORAGE_ENTRIES, JSON.stringify(data));
}

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

function fetchHabits() {
    return Promise.resolve(loadHabitsFromStorage());
}

function addHabit(name) {
    const list = loadHabitsFromStorage();
    const nextId = list.length > 0 ? Math.max(...list.map(h => h.id)) + 1 : 1;
    const habit = { id: nextId, name: name.trim() };
    list.push(habit);
    saveHabitsToStorage(list);
    return Promise.resolve(habit);
}

function editHabit(id, newName) {
    const list = loadHabitsFromStorage();
    const habit = list.find(h => h.id === id);
    if (!habit || !newName?.trim()) return;
    habit.name = newName.trim();
    saveHabitsToStorage(list);
}

function deleteHabit(id) {
    const list = loadHabitsFromStorage().filter(h => h.id !== id);
    saveHabitsToStorage(list);
    const entriesData = loadEntriesFromStorage();
    delete entriesData[id];
    saveEntriesToStorage(entriesData);
}

function logEntry(habitId, dateStr, completed) {
    const data = loadEntriesFromStorage();
    if (!data[habitId]) data[habitId] = {};
    data[habitId][dateStr] = completed;
    saveEntriesToStorage(data);
    return Promise.resolve({ habitId, date: dateStr, completed });
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

function fetchGraphData() {
    const list = loadHabitsFromStorage();
    const entriesData = loadEntriesFromStorage();
    const total = list.length || 1;

    if (graphMode === 'weekly') {
        const start = getWeekStart(selectedDate);
        const dayNames = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
        return dayNames.map((day, i) => {
            const d = new Date(start);
            d.setDate(start.getDate() + i);
            const dateStr = getDateStr(d);
            let completed = 0;
            list.forEach(h => {
                if (entriesData[h.id]?.[dateStr] === true) completed++;
            });
            return { day, completedCount: completed, totalHabits: total };
        });
    } else {
        const year = selectedDate.getFullYear();
        const month = selectedDate.getMonth();
        const daysInMonth = new Date(year, month + 1, 0).getDate();
        const result = [];
        for (let day = 1; day <= daysInMonth; day++) {
            const dateStr = `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
            let completed = 0;
            list.forEach(h => {
                if (entriesData[h.id]?.[dateStr] === true) completed++;
            });
            result.push({ day: String(day), completedCount: completed, totalHabits: total });
        }
        return result;
    }
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
        `<div class="habit-item" data-id="${h.id}">
            <div class="habit-bubble" title="${escapeHtml(h.name)}">${escapeHtml(h.name)}</div>
            <div class="habit-actions">
                <button type="button" class="habit-btn habit-btn-edit" title="Edit habit">Edit</button>
                <button type="button" class="habit-btn habit-btn-delete" title="Delete habit and all records">Delete</button>
            </div>
        </div>`
    ).join('');

    list.querySelectorAll('.habit-btn-edit').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.stopPropagation();
            const item = btn.closest('.habit-item');
            const id = parseInt(item.dataset.id);
            const habit = habits.find(h => h.id === id);
            if (habit) showEditHabitModal(id, habit.name);
        });
    });
    list.querySelectorAll('.habit-btn-delete').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.stopPropagation();
            const item = btn.closest('.habit-item');
            const id = parseInt(item.dataset.id);
            handleDeleteHabit(id);
        });
    });
}

function showEditHabitModal(id, currentName) {
    const modal = document.getElementById('editHabitModal');
    document.getElementById('editHabitId').value = id;
    document.getElementById('editHabitName').value = currentName;
    modal.style.display = 'flex';
}

function hideEditHabitModal() {
    document.getElementById('editHabitModal').style.display = 'none';
}

async function handleEditHabitSubmit(e) {
    e.preventDefault();
    const id = parseInt(document.getElementById('editHabitId').value);
    const newName = document.getElementById('editHabitName').value?.trim();
    if (!newName) return;
    editHabit(id, newName);
    hideEditHabitModal();
    await loadAndRender();
}

async function handleDeleteHabit(id) {
    const habit = habits.find(h => h.id === id);
    const name = habit ? habit.name : 'this habit';
    if (!confirm(`Delete "${name}" and all its tracking records? This cannot be undone.`)) return;
    deleteHabit(id);
    await loadAndRender();
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
        cell.addEventListener('click', () => {
            const habitId = parseInt(cell.dataset.habitId);
            const dateStr = cell.dataset.date;
            const current = getEntry(habitId, dateStr);
            let next = true;
            if (current === true) next = false;
            else if (current === false) next = true;
            setEntry(habitId, dateStr, next);
            try {
                logEntry(habitId, dateStr, next);
                renderTracker();
                renderGraph();
                loadAwards();
            } catch (e) {
                setEntry(habitId, dateStr, current);
                renderTracker();
                alert('Could not save to browser storage.');
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
    habits = loadHabitsFromStorage();
    entries = loadEntriesFromStorage();
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

function fetchAwards() {
    const list = loadHabitsFromStorage();
    const entriesData = loadEntriesFromStorage();
    const year = selectedDate.getFullYear();
    const month = selectedDate.getMonth();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const fromStr = `${year}-${String(month + 1).padStart(2, '0')}-01`;
    const toStr = `${year}-${String(month + 1).padStart(2, '0')}-${String(daysInMonth).padStart(2, '0')}`;

    if (list.length === 0) return { topActivity: null, lowestActivity: null, highestStreakActivity: null };

    const completedByHabit = {};
    list.forEach(h => { completedByHabit[h.id] = 0; });

    list.forEach(h => {
        const dates = entriesData[h.id] || {};
        for (let d = 1; d <= daysInMonth; d++) {
            const dateStr = `${year}-${String(month + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`;
            if (dates[dateStr] === true) completedByHabit[h.id]++;
        }
    });

    const sorted = list.map(h => ({ ...h, count: completedByHabit[h.id] })).sort((a, b) => b.count - a.count);
    const top = sorted[0]?.count > 0 ? sorted[0] : null;
    const low = sorted.length > 0 ? sorted[sorted.length - 1] : null;

    let bestStreak = 0, streakHabit = null;
    list.forEach(h => {
        const dates = (entriesData[h.id] || {});
        const completedDates = Object.entries(dates)
            .filter(([, v]) => v === true)
            .map(([d]) => d)
            .filter(d => d >= fromStr && d <= toStr)
            .sort();
        let maxStreak = 0, streak = 1;
        for (let i = 1; i < completedDates.length; i++) {
            const diff = (new Date(completedDates[i]) - new Date(completedDates[i - 1])) / 86400000;
            if (diff === 1) streak++;
            else { maxStreak = Math.max(maxStreak, streak); streak = 1; }
        }
        maxStreak = Math.max(maxStreak, completedDates.length > 0 ? streak : 0);
        if (maxStreak > bestStreak) { bestStreak = maxStreak; streakHabit = h; }
    });

    return {
        topActivity: top ? { name: top.name, count: top.count } : null,
        lowestActivity: low ? { name: low.name, count: low.count } : null,
        highestStreakActivity: streakHabit ? { name: streakHabit.name, count: bestStreak } : null
    };
}

async function loadAwards() {
    const container = document.getElementById('awardsContainer');
    if (!container) return;
    const awards = fetchAwards();
    renderAwards(awards);
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

function initEditHabitModal() {
    const modal = document.getElementById('editHabitModal');
    if (!modal) return;
    modal.querySelectorAll('[data-dismiss="editHabitModal"]').forEach(el => {
        el.addEventListener('click', hideEditHabitModal);
    });
    document.getElementById('editHabitForm')?.addEventListener('submit', handleEditHabitSubmit);
}

initDatePicker();
initGraphModeToggle();
initEditHabitModal();
initChat();
loadAndRender();
