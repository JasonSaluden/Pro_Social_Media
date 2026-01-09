// common.js - Fonctions et utilitaires partagés

// Récupère les données d'authentification
// Note: Le token JWT est maintenant dans un cookie HttpOnly (non accessible par JS)
// On garde les infos user dans localStorage pour l'affichage seulement
function getAuthData() {
    const user = JSON.parse(localStorage.getItem('user') || 'null');
    return { user };
}

// Vérifie si l'utilisateur est connecté, redirige sinon
function requireAuth() {
    const { user } = getAuthData();
    if (!user) {
        window.location.href = '/AuthView/Login';
        return null;
    }
    return { user };
}

// Effectue une requête API authentifiée
// Le cookie HttpOnly est envoyé automatiquement avec credentials: 'same-origin'
async function apiRequest(url, options = {}) {
    const defaultOptions = {
        headers: {
            'Content-Type': 'application/json'
        },
        credentials: 'same-origin' // Envoie automatiquement les cookies HttpOnly
    };

    const mergedOptions = {
        ...defaultOptions,
        ...options,
        headers: {
            ...defaultOptions.headers,
            ...options.headers
        },
        credentials: 'same-origin'
    };

    const response = await fetch(url, mergedOptions);

    // Si non autorisé, déconnexion
    if (response.status === 401) {
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

// Échappe le HTML pour éviter les XSS
function escapeHtml(text) {
    if (text === null || text === undefined) return '';
    const div = document.createElement('div');
    div.textContent = String(text);
    return div.innerHTML;
}
