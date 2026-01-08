// network.js - Page Réseau

document.addEventListener('DOMContentLoaded', function() {
    const auth = requireAuth();
    if (!auth) return;

    // Charge les suggestions au démarrage
    loadSuggestions();

    // Charge les autres onglets quand on clique dessus
    document.getElementById('connections-tab').addEventListener('shown.bs.tab', loadConnections);
    document.getElementById('pending-tab').addEventListener('shown.bs.tab', loadPending);
});

function createUserCard(user, actions = '') {
    return `
        <div class="col-md-4 mb-3">
            <div class="card h-100 user-card">
                <div class="card-body text-center">
                    <img src="${getAvatarUrl(user.avatarUrl, 64)}"
                         class="rounded-circle mb-3" width="64" height="64" alt="Avatar">
                    <h6 class="card-title">${user.firstName} ${user.lastName}</h6>
                    <p class="text-muted small">${user.headline || ''}</p>
                    ${actions}
                </div>
            </div>
        </div>`;
}

async function loadSuggestions() {
    const container = document.getElementById('suggestions-container');

    try {
        const response = await apiRequest('/api/connections/suggestions');
        if (!response) return;

        const users = await response.json();

        if (users.length === 0) {
            container.innerHTML = '<p class="text-muted">Aucune suggestion pour le moment</p>';
            return;
        }

        container.innerHTML = users.map(user => createUserCard(user, `
            <button class="btn btn-primary btn-sm" onclick="sendRequest('${user.id}')">
                <i class="bi bi-person-plus me-1"></i>Se connecter
            </button>
        `)).join('');
    } catch (error) {
        container.innerHTML = '<p class="text-danger">Erreur de chargement</p>';
    }
}

async function loadConnections() {
    const container = document.getElementById('connections-container');

    try {
        const response = await apiRequest('/api/connections');
        if (!response) return;

        const connections = await response.json();

        if (connections.length === 0) {
            container.innerHTML = '<p class="text-muted">Aucune connexion pour le moment</p>';
            return;
        }

        // Le DTO retourne 'user' pas 'connectedUser'
        container.innerHTML = connections.map(c => createUserCard(c.user, '')).join('');
    } catch (error) {
        container.innerHTML = '<p class="text-danger">Erreur de chargement</p>';
    }
}

async function loadPending() {
    const container = document.getElementById('pending-container');

    try {
        // L'endpoint est /pending, pas /requests
        const response = await apiRequest('/api/connections/pending');
        if (!response) return;

        const requests = await response.json();

        if (requests.length === 0) {
            container.innerHTML = '<p class="text-muted">Aucune demande en attente</p>';
            return;
        }

        container.innerHTML = requests.map(r => createUserCard(r.requester, `
            <div class="btn-group">
                <button class="btn btn-success btn-sm" onclick="acceptRequest('${r.id}')">
                    <i class="bi bi-check"></i>
                </button>
                <button class="btn btn-danger btn-sm" onclick="rejectRequest('${r.id}')">
                    <i class="bi bi-x"></i>
                </button>
            </div>
        `)).join('');
    } catch (error) {
        container.innerHTML = '<p class="text-danger">Erreur de chargement</p>';
    }
}

async function sendRequest(userId) {
    await apiRequest(`/api/connections/request/${userId}`, { method: 'POST' });
    loadSuggestions();
}

async function acceptRequest(requestId) {
    // L'endpoint est PUT /{id}/accept
    await apiRequest(`/api/connections/${requestId}/accept`, { method: 'PUT' });
    loadPending();
    loadConnections();
}

async function rejectRequest(requestId) {
    // L'endpoint est PUT /{id}/reject
    await apiRequest(`/api/connections/${requestId}/reject`, { method: 'PUT' });
    loadPending();
}
