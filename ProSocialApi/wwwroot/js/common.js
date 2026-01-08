// common.js - Fonctions et utilitaires partagés

// Récupère les données d'authentification
function getAuthData() {
    const token = localStorage.getItem('token');
    const user = JSON.parse(localStorage.getItem('user') || 'null');
    return { token, user };
}

// Vérifie si l'utilisateur est connecté, redirige sinon
function requireAuth() {
    const { token, user } = getAuthData();
    if (!token || !user) {
        window.location.href = '/AuthView/Login';
        return null;
    }
    return { token, user };
}

// Effectue une requête API authentifiée
async function apiRequest(url, options = {}) {
    const { token } = getAuthData();

    const defaultOptions = {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        }
    };

    const mergedOptions = {
        ...defaultOptions,
        ...options,
        headers: {
            ...defaultOptions.headers,
            ...options.headers
        }
    };

    const response = await fetch(url, mergedOptions);

    // Si non autorisé, déconnexion
    if (response.status === 401) {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        window.location.href = '/AuthView/Login';
        return null;
    }

    return response;
}

// Formate une date en français
function formatDate(dateString, includeTime = true) {
    const options = {
        day: 'numeric',
        month: 'short',
        year: 'numeric'
    };

    if (includeTime) {
        options.hour = '2-digit';
        options.minute = '2-digit';
    }

    return new Date(dateString).toLocaleDateString('fr-FR', options);
}

// Affiche une alerte
function showAlert(elementId, message, type = 'danger') {
    const alert = document.getElementById(elementId);
    if (alert) {
        alert.className = `alert alert-${type}`;
        alert.textContent = message;
        alert.classList.remove('d-none');
    }
}

// Cache une alerte
function hideAlert(elementId) {
    const alert = document.getElementById(elementId);
    if (alert) {
        alert.classList.add('d-none');
    }
}

// Gère l'état d'un bouton de soumission
function setButtonLoading(buttonId, isLoading, loadingText = 'Chargement...', normalText = 'Envoyer') {
    const btn = document.getElementById(buttonId);
    const btnText = btn?.querySelector('[data-btn-text]') || document.getElementById(buttonId + '-text');
    const btnSpinner = btn?.querySelector('[data-btn-spinner]') || document.getElementById(buttonId + '-spinner');

    if (btn) {
        btn.disabled = isLoading;
    }
    if (btnText) {
        btnText.textContent = isLoading ? loadingText : normalText;
    }
    if (btnSpinner) {
        btnSpinner.classList.toggle('d-none', !isLoading);
    }
}

// Avatar par défaut
function getAvatarUrl(url, size = 48) {
    return url || `https://via.placeholder.com/${size}`;
}

// Met à jour le localStorage user
function updateStoredUser(userData) {
    localStorage.setItem('user', JSON.stringify(userData));
}
