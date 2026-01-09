// register.js - Page d'inscription

document.addEventListener('DOMContentLoaded', function() {
    // Si déjà connecté, rediriger vers l'accueil
    const { user } = getAuthData();
    if (user) {
        window.location.href = '/Home/Index';
        return;
    }

    document.getElementById('register-form').addEventListener('submit', handleRegister);
});

async function handleRegister(e) {
    e.preventDefault();

    const submitBtn = document.getElementById('submit-btn');
    const btnText = document.getElementById('btn-text');
    const btnSpinner = document.getElementById('btn-spinner');

    const password = document.getElementById('password').value;
    const confirmPassword = document.getElementById('confirmPassword').value;

    // Validation mot de passe
    if (password !== confirmPassword) {
        showAlert('error-alert', 'Les mots de passe ne correspondent pas');
        return;
    }

    // Désactive le bouton et affiche le spinner
    submitBtn.disabled = true;
    btnText.textContent = 'Inscription...';
    btnSpinner.classList.remove('d-none');
    hideAlert('error-alert');

    const data = {
        firstName: document.getElementById('firstName').value,
        lastName: document.getElementById('lastName').value,
        email: document.getElementById('email').value,
        password: password
    };

    try {
        const response = await fetch('/api/auth/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'same-origin', // Accepte le cookie HttpOnly du serveur
            body: JSON.stringify(data)
        });

        const result = await response.json();

        if (result.success && result.user) {
            // Stocker uniquement les infos user (le token est dans un cookie HttpOnly)
            localStorage.setItem('user', JSON.stringify(result.user));
            window.location.href = '/Home/Index';
        } else {
            showAlert('error-alert', result.message || "Erreur lors de l'inscription");
        }
    } catch (error) {
        showAlert('error-alert', 'Erreur de connexion au serveur');
    } finally {
        submitBtn.disabled = false;
        btnText.textContent = "S'inscrire";
        btnSpinner.classList.add('d-none');
    }
}
