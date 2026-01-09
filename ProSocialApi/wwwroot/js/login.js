// login.js - Page de connexion

document.addEventListener('DOMContentLoaded', function() {
    // Si déjà connecté, rediriger vers l'accueil
    const { user } = getAuthData();
    if (user) {
        window.location.href = '/Home/Index';
        return;
    }

    document.getElementById('login-form').addEventListener('submit', handleLogin);
});

async function handleLogin(e) {
    e.preventDefault();

    const submitBtn = document.getElementById('submit-btn');
    const btnText = document.getElementById('btn-text');
    const btnSpinner = document.getElementById('btn-spinner');

    // Désactive le bouton et affiche le spinner
    submitBtn.disabled = true;
    btnText.textContent = 'Connexion...';
    btnSpinner.classList.remove('d-none');
    hideAlert('error-alert');

    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;

    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'same-origin', // Accepte le cookie HttpOnly du serveur
            body: JSON.stringify({ email, password })
        });

        const data = await response.json();

        if (data.success && data.user) {
            // Stocker uniquement les infos user (le token est dans un cookie HttpOnly)
            localStorage.setItem('user', JSON.stringify(data.user));
            window.location.href = '/Home/Index';
        } else {
            showAlert('error-alert', data.message || 'Erreur de connexion');
        }
    } catch (error) {
        showAlert('error-alert', 'Erreur de connexion au serveur');
    } finally {
        submitBtn.disabled = false;
        btnText.textContent = 'Se connecter';
        btnSpinner.classList.add('d-none');
    }
}
