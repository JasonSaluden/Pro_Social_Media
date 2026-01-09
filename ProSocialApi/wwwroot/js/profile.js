// profile.js - Page Profil

let currentUser = null;

document.addEventListener('DOMContentLoaded', function() {
    const auth = requireAuth();
    if (!auth) return;

    currentUser = auth.user;

    // Affiche les infos initiales
    displayProfile();

    // Charge le profil complet et les stats
    loadFullProfile();
    loadStats();

    // Formulaire de modification
    document.getElementById('profile-form').addEventListener('submit', handleUpdateProfile);

    // Upload d'avatar
    document.getElementById('avatar-input').addEventListener('change', handleAvatarUpload);
});

function displayProfile() {
    document.getElementById('profile-name').textContent = `${currentUser.firstName} ${currentUser.lastName}`;
    document.getElementById('profile-headline').textContent = currentUser.headline || 'Membre Pro Social';
    document.getElementById('profile-email').textContent = currentUser.email;

    if (currentUser.avatarUrl) {
        document.getElementById('profile-avatar').src = currentUser.avatarUrl;
    }

    // Remplit le formulaire
    document.getElementById('firstName').value = currentUser.firstName || '';
    document.getElementById('lastName').value = currentUser.lastName || '';
    document.getElementById('headline').value = currentUser.headline || '';
    document.getElementById('bio').value = currentUser.bio || '';
}

async function loadFullProfile() {
    try {
        const response = await apiRequest('/api/users/me');
        if (!response || !response.ok) return;

        const fullUser = await response.json();
        updateStoredUser(fullUser);
        Object.assign(currentUser, fullUser);
        displayProfile();
    } catch (error) {
        console.error('Erreur chargement profil:', error);
    }
}

async function loadStats() {
    try {
        const [connectionsRes, postsRes] = await Promise.all([
            apiRequest('/api/connections'),
            apiRequest('/api/posts/user/' + currentUser.id)
        ]);

        if (connectionsRes && connectionsRes.ok) {
            const connections = await connectionsRes.json();
            document.getElementById('connections-count').textContent = connections.length;
        }

        if (postsRes && postsRes.ok) {
            const posts = await postsRes.json();
            document.getElementById('posts-count').textContent = posts.length;
            displayPosts(posts);
        }
    } catch (error) {
        console.error('Erreur chargement stats:', error);
    }
}

function displayPosts(posts) {
    const container = document.getElementById('my-posts-container');

    if (posts.length === 0) {
        container.innerHTML = '<p class="text-muted text-center">Aucune publication</p>';
        return;
    }

    container.innerHTML = posts.map(post => {
        const date = formatDate(post.createdAt, false);
        return `
            <div class="border-bottom pb-3 mb-3">
                <p class="mb-1">${escapeHtml(post.content)}</p>
                <small class="text-muted">${escapeHtml(date)} - ${post.likesCount || 0} likes, ${post.commentsCount || 0} commentaires</small>
            </div>`;
    }).join('');
}

async function handleUpdateProfile(e) {
    e.preventDefault();

    hideAlert('success-alert');
    hideAlert('error-alert');

    const data = {
        firstName: document.getElementById('firstName').value,
        lastName: document.getElementById('lastName').value,
        headline: document.getElementById('headline').value,
        bio: document.getElementById('bio').value
    };

    try {
        const response = await apiRequest('/api/users/me', {
            method: 'PUT',
            body: JSON.stringify(data)
        });

        if (response && response.ok) {
            const updatedUser = await response.json();
            updateStoredUser(updatedUser);
            Object.assign(currentUser, updatedUser);
            displayProfile();
            showAlert('success-alert', 'Profil mis a jour avec succes !', 'success');
        } else {
            const error = await response.json();
            showAlert('error-alert', error.message || 'Erreur lors de la mise a jour');
        }
    } catch (error) {
        showAlert('error-alert', 'Erreur de connexion au serveur');
    }
}

async function handleAvatarUpload(e) {
    const file = e.target.files[0];
    if (!file) return;

    const statusEl = document.getElementById('avatar-upload-status');
    const avatarEl = document.getElementById('profile-avatar');

    // Validation côté client
    if (file.size > 5 * 1024 * 1024) {
        statusEl.innerHTML = '<span class="text-danger">Le fichier ne doit pas depasser 5 Mo</span>';
        return;
    }

    const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
        statusEl.innerHTML = '<span class="text-danger">Format non supporte (jpg, png, gif, webp)</span>';
        return;
    }

    // Affiche un aperçu immédiat
    const reader = new FileReader();
    reader.onload = (e) => {
        avatarEl.src = e.target.result;
    };
    reader.readAsDataURL(file);

    // Upload
    statusEl.innerHTML = '<span class="text-muted"><i class="bi bi-arrow-repeat spin"></i> Upload en cours...</span>';

    const formData = new FormData();
    formData.append('file', file);

    try {
        const { token } = getAuthData();
        const response = await fetch('/api/users/me/avatar', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${token}`
            },
            body: formData
        });

        if (response.ok) {
            const data = await response.json();
            currentUser.avatarUrl = data.avatarUrl;
            updateStoredUser(currentUser);
            avatarEl.src = data.avatarUrl;
            statusEl.innerHTML = '<span class="text-success"><i class="bi bi-check"></i> Photo mise a jour</span>';

            // Efface le message après 3 secondes
            setTimeout(() => {
                statusEl.innerHTML = '';
            }, 3000);
        } else {
            const error = await response.json();
            statusEl.innerHTML = `<span class="text-danger">${error.message || 'Erreur upload'}</span>`;
            // Remet l'ancienne image
            avatarEl.src = currentUser.avatarUrl || getAvatarUrl(null, 120);
        }
    } catch (error) {
        statusEl.innerHTML = '<span class="text-danger">Erreur de connexion</span>';
        avatarEl.src = currentUser.avatarUrl || getAvatarUrl(null, 120);
    }

    // Reset l'input pour permettre de re-sélectionner le même fichier
    e.target.value = '';
}
