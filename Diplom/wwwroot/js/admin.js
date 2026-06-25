// ===== ПЕРЕКЛЮЧЕНИЕ ВКЛАДОК =====
document.querySelectorAll('.tab-btn').forEach(btn => {
    btn.addEventListener('click', function() {
        document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        this.classList.add('active');

        document.querySelectorAll('.tab-content').forEach(t => t.classList.remove('active'));
        document.getElementById('tab-' + this.dataset.tab).classList.add('active');
    });
});

// ===== МОДАЛКА: ДОБАВЛЕНИЕ ПОЛЬЗОВАТЕЛЯ =====
const addUserModal = document.getElementById('addUserModal');
const openAddUserBtn = document.getElementById('openAddUserModal');

if (openAddUserBtn) {
    openAddUserBtn.addEventListener('click', () => addUserModal.classList.add('active'));
}

document.getElementById('closeAddUserModal')?.addEventListener('click', () => addUserModal.classList.remove('active'));
document.getElementById('closeAddUserModalBtn')?.addEventListener('click', () => addUserModal.classList.remove('active'));

if (addUserModal) {
    addUserModal.addEventListener('click', (e) => {
        if (e.target === addUserModal) addUserModal.classList.remove('active');
    });
}

// ===== МОДАЛКА: ДОБАВЛЕНИЕ ТИПА РАБОТЫ =====
const addWorkTypeModal = document.getElementById('addWorkTypeModal');
const openAddWorkTypeBtn = document.getElementById('openAddWorkTypeModal');

if (openAddWorkTypeBtn) {
    openAddWorkTypeBtn.addEventListener('click', () => addWorkTypeModal.classList.add('active'));
}

document.getElementById('closeAddWorkTypeModal')?.addEventListener('click', () => addWorkTypeModal.classList.remove('active'));
document.getElementById('closeAddWorkTypeModalBtn')?.addEventListener('click', () => addWorkTypeModal.classList.remove('active'));

if (addWorkTypeModal) {
    addWorkTypeModal.addEventListener('click', (e) => {
        if (e.target === addWorkTypeModal) addWorkTypeModal.classList.remove('active');
    });
}

// ===== АВТОМАТИЧЕСКОЕ ЗАКРЫТИЕ МОДАЛОК ПРИ УСПЕХЕ =====
if (document.querySelector('.alert-success')) {
    setTimeout(() => {
        document.querySelectorAll('.modal-overlay.active').forEach(m => m.classList.remove('active'));
    }, 2000);
}