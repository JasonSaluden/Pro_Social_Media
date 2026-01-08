// messages.js - Gestion de la messagerie

let currentConversationId = null;
let currentParticipant = null;
let conversations = [];
let searchTimeout = null;

// Initialisation au chargement de la page
document.addEventListener('DOMContentLoaded', async () => {
    const auth = requireAuth();
    if (!auth) return;

    await loadConversations();
    setupEventListeners();
});

// Configuration des event listeners
function setupEventListeners() {
    // Envoi de message
    document.getElementById('send-message-form').addEventListener('submit', handleSendMessage);

    // Recherche d'utilisateur (avec debounce)
    document.getElementById('search-user-input').addEventListener('input', (e) => {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => searchUsers(e.target.value), 300);
    });

    // Effacer l'utilisateur sélectionné
    document.getElementById('clear-selected-user').addEventListener('click', clearSelectedUser);

    // Démarrer une conversation
    document.getElementById('start-conversation-btn').addEventListener('click', startConversation);

    // Reset modal à la fermeture
    document.getElementById('newConversationModal').addEventListener('hidden.bs.modal', resetNewConversationModal);
}

// Charger la liste des conversations
async function loadConversations() {
    const container = document.getElementById('conversations-list');

    try {
        const response = await apiRequest('/api/conversations');
        if (!response.ok) throw new Error('Erreur chargement');

        conversations = await response.json();

        if (conversations.length === 0) {
            container.innerHTML = `
                <div class="text-center text-muted py-4">
                    <i class="bi bi-chat-square display-6 mb-2"></i>
                    <p class="small">Aucune conversation</p>
                </div>
            `;
            return;
        }

        container.innerHTML = conversations.map(conv => renderConversationItem(conv)).join('');

        // Ajouter les event listeners aux conversations
        document.querySelectorAll('.conversation-item').forEach(item => {
            item.addEventListener('click', () => selectConversation(item.dataset.conversationId));
        });

    } catch (error) {
        console.error('Erreur:', error);
        container.innerHTML = `
            <div class="text-center text-danger py-4">
                <i class="bi bi-exclamation-circle"></i>
                <p class="small">Erreur de chargement</p>
            </div>
        `;
    }
}

// Rendre un élément de conversation dans la liste
function renderConversationItem(conv) {
    const { user } = getAuthData();
    const participant = conv.participants.find(p => p.id !== user.id) || conv.participants[0];
    const lastMessage = conv.lastMessage?.content || 'Aucun message';
    const lastMessageTime = conv.lastMessageAt ? formatRelativeTime(conv.lastMessageAt) : '';

    return `
        <div class="conversation-item p-3 border-bottom d-flex align-items-center"
             data-conversation-id="${conv.id}">
            <img src="${getAvatarUrl(participant.avatarUrl, 48)}"
                 class="rounded-circle me-3" width="48" height="48" alt="Avatar">
            <div class="flex-grow-1 overflow-hidden">
                <div class="d-flex justify-content-between align-items-center">
                    <h6 class="mb-0 text-truncate">${participant.firstName} ${participant.lastName}</h6>
                    <small class="text-muted">${lastMessageTime}</small>
                </div>
                <p class="mb-0 text-muted small text-truncate">${lastMessage}</p>
            </div>
        </div>
    `;
}

// Sélectionner une conversation
async function selectConversation(conversationId) {
    currentConversationId = conversationId;

    // Mettre à jour l'UI
    document.querySelectorAll('.conversation-item').forEach(item => {
        item.classList.toggle('active', item.dataset.conversationId === conversationId);
    });

    document.getElementById('no-conversation-selected').classList.add('d-none');
    document.getElementById('conversation-content').classList.remove('d-none');
    document.getElementById('conversation-content').classList.add('d-flex');

    // Charger les messages
    await loadMessages(conversationId);
}

// Charger les messages d'une conversation
async function loadMessages(conversationId) {
    const container = document.getElementById('messages-container');
    container.innerHTML = `
        <div class="text-center py-4">
            <div class="spinner-border spinner-border-sm text-primary" role="status"></div>
        </div>
    `;

    try {
        const response = await apiRequest(`/api/conversations/${conversationId}`);
        if (!response.ok) throw new Error('Erreur chargement');

        const conversation = await response.json();
        const { user } = getAuthData();

        // Trouver le participant (l'autre personne)
        currentParticipant = conversation.participants.find(p => p.id !== user.id) || conversation.participants[0];

        // Mettre à jour le header
        document.getElementById('chat-participant-avatar').src = getAvatarUrl(currentParticipant.avatarUrl, 40);
        document.getElementById('chat-participant-name').textContent = `${currentParticipant.firstName} ${currentParticipant.lastName}`;
        document.getElementById('chat-participant-headline').textContent = currentParticipant.headline || '';

        // Rendre les messages
        if (conversation.messages.length === 0) {
            container.innerHTML = `
                <div class="text-center text-muted py-4">
                    <p>Aucun message. Commencez la conversation !</p>
                </div>
            `;
        } else {
            container.innerHTML = conversation.messages.map(msg => renderMessage(msg, user.id)).join('');
            // Scroll en bas
            container.scrollTop = container.scrollHeight;
        }

    } catch (error) {
        console.error('Erreur:', error);
        container.innerHTML = `
            <div class="text-center text-danger py-4">
                <i class="bi bi-exclamation-circle"></i>
                <p>Erreur de chargement des messages</p>
            </div>
        `;
    }
}

// Rendre un message
function renderMessage(message, currentUserId) {
    const isSent = message.senderId === currentUserId;
    const time = formatRelativeTime(message.sentAt);

    return `
        <div class="d-flex ${isSent ? 'justify-content-end' : 'justify-content-start'}">
            <div class="message-bubble ${isSent ? 'message-sent' : 'message-received'}">
                <div>${escapeHtml(message.content)}</div>
                <div class="message-time text-end">${time}</div>
            </div>
        </div>
    `;
}

// Envoyer un message
async function handleSendMessage(e) {
    e.preventDefault();

    if (!currentConversationId) return;

    const input = document.getElementById('message-input');
    const content = input.value.trim();

    if (!content) return;

    try {
        const response = await apiRequest(`/api/conversations/${currentConversationId}/messages`, {
            method: 'POST',
            body: JSON.stringify({ content })
        });

        if (!response.ok) throw new Error('Erreur envoi');

        // Vider l'input
        input.value = '';

        // Recharger les messages
        await loadMessages(currentConversationId);

        // Recharger la liste des conversations (pour mettre à jour le dernier message)
        await loadConversations();

        // Re-sélectionner la conversation active
        document.querySelector(`[data-conversation-id="${currentConversationId}"]`)?.classList.add('active');

    } catch (error) {
        console.error('Erreur:', error);
        alert('Erreur lors de l\'envoi du message');
    }
}

// Rechercher des utilisateurs
async function searchUsers(query) {
    const container = document.getElementById('search-results');

    if (query.length < 2) {
        container.innerHTML = '';
        return;
    }

    try {
        const response = await apiRequest(`/api/users/search?q=${encodeURIComponent(query)}`);
        if (!response.ok) throw new Error('Erreur recherche');

        const users = await response.json();
        const { user: currentUser } = getAuthData();

        // Filtrer l'utilisateur courant
        const filteredUsers = users.filter(u => u.id !== currentUser.id);

        if (filteredUsers.length === 0) {
            container.innerHTML = `<p class="text-muted small text-center">Aucun utilisateur trouvé</p>`;
            return;
        }

        container.innerHTML = filteredUsers.map(user => `
            <div class="search-result-item d-flex align-items-center p-2 border-bottom"
                 data-user-id="${user.id}"
                 data-user-name="${user.firstName} ${user.lastName}"
                 data-user-avatar="${user.avatarUrl || ''}">
                <img src="${getAvatarUrl(user.avatarUrl, 32)}" class="rounded-circle me-2" width="32" height="32" alt="">
                <div>
                    <div class="fw-medium">${user.firstName} ${user.lastName}</div>
                    <small class="text-muted">${user.headline || ''}</small>
                </div>
            </div>
        `).join('');

        // Event listeners pour la sélection
        container.querySelectorAll('.search-result-item').forEach(item => {
            item.addEventListener('click', () => selectUser(item));
        });

    } catch (error) {
        console.error('Erreur:', error);
        container.innerHTML = `<p class="text-danger small text-center">Erreur de recherche</p>`;
    }
}

// Sélectionner un utilisateur pour la nouvelle conversation
function selectUser(item) {
    const userId = item.dataset.userId;
    const userName = item.dataset.userName;
    const userAvatar = item.dataset.userAvatar;

    document.getElementById('selected-user-id').value = userId;
    document.getElementById('selected-user-name').textContent = userName;
    document.getElementById('selected-user-avatar').src = getAvatarUrl(userAvatar, 32);
    document.getElementById('selected-user-container').classList.remove('d-none');
    document.getElementById('search-results').innerHTML = '';
    document.getElementById('search-user-input').value = '';
    document.getElementById('start-conversation-btn').disabled = false;
}

// Effacer l'utilisateur sélectionné
function clearSelectedUser() {
    document.getElementById('selected-user-id').value = '';
    document.getElementById('selected-user-container').classList.add('d-none');
    document.getElementById('start-conversation-btn').disabled = true;
}

// Démarrer une nouvelle conversation
async function startConversation() {
    const participantId = document.getElementById('selected-user-id').value;
    const initialMessage = document.getElementById('initial-message').value.trim();

    if (!participantId || !initialMessage) {
        alert('Veuillez sélectionner un destinataire et écrire un message');
        return;
    }

    const btn = document.getElementById('start-conversation-btn');
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Envoi...';

    try {
        const response = await apiRequest('/api/conversations', {
            method: 'POST',
            body: JSON.stringify({ participantId, initialMessage })
        });

        if (!response.ok) throw new Error('Erreur création');

        const conversation = await response.json();

        // Fermer la modal
        bootstrap.Modal.getInstance(document.getElementById('newConversationModal')).hide();

        // Recharger les conversations
        await loadConversations();

        // Sélectionner la nouvelle conversation
        await selectConversation(conversation.id);

    } catch (error) {
        console.error('Erreur:', error);
        alert('Erreur lors de la création de la conversation');
    } finally {
        btn.disabled = false;
        btn.innerHTML = '<i class="bi bi-send me-1"></i>Envoyer';
    }
}

// Reset la modal de nouvelle conversation
function resetNewConversationModal() {
    document.getElementById('search-user-input').value = '';
    document.getElementById('search-results').innerHTML = '';
    document.getElementById('initial-message').value = '';
    clearSelectedUser();
}

// Formater une date en temps relatif
function formatRelativeTime(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return "À l'instant";
    if (diffMins < 60) return `${diffMins} min`;
    if (diffHours < 24) return `${diffHours}h`;
    if (diffDays < 7) return `${diffDays}j`;

    return date.toLocaleDateString('fr-FR', { day: 'numeric', month: 'short' });
}

// Échapper le HTML pour éviter les XSS
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
