// feed.js - Page d'accueil / Feed

document.addEventListener('DOMContentLoaded', function() {
    const auth = requireAuth();
    if (!auth) return;

    const { user } = auth;

    // Affiche les infos utilisateur dans la sidebar
    initUserInfo(user);

    // Charge le feed
    loadFeed();

    // Formulaire de création de post
    document.getElementById('create-post-form').addEventListener('submit', handleCreatePost);
});

function initUserInfo(user) {
    document.getElementById('sidebar-name').textContent = `${user.firstName} ${user.lastName}`;
    document.getElementById('sidebar-headline').textContent = user.headline || 'Membre Pro Social';

    if (user.avatarUrl) {
        document.getElementById('user-avatar').src = user.avatarUrl;
        document.getElementById('sidebar-avatar').src = user.avatarUrl;
    }
}

async function loadFeed() {
    const container = document.getElementById('feed-container');

    try {
        const response = await apiRequest('/api/feed');
        if (!response) return;

        const posts = await response.json();

        if (posts.length === 0) {
            container.innerHTML = `
                <div class="text-center py-5 text-muted">
                    <i class="bi bi-inbox display-4"></i>
                    <p class="mt-3">Aucun post pour le moment</p>
                </div>`;
            return;
        }

        container.innerHTML = posts.map(post => createPostCard(post)).join('');
    } catch (error) {
        container.innerHTML = `
            <div class="alert alert-danger">
                Erreur lors du chargement du feed
            </div>`;
    }
}

function createPostCard(post) {
    const date = formatDate(post.createdAt);
    const isLiked = post.isLikedByCurrentUser || false;
    const likeButtonClass = isLiked ? 'btn-primary' : 'btn-outline-primary';
    const likeIconClass = isLiked ? 'bi-hand-thumbs-up-fill' : 'bi-hand-thumbs-up';

    return `
        <div class="card mb-3" id="post-${post.id}">
            <div class="card-body">
                <div class="d-flex gap-3 mb-3">
                    <img src="${getAvatarUrl(post.author?.avatarUrl)}"
                         class="rounded-circle" width="48" height="48" alt="Avatar">
                    <div>
                        <h6 class="mb-0">${escapeHtml(post.author?.firstName)} ${escapeHtml(post.author?.lastName)}</h6>
                        <small class="text-muted">${escapeHtml(post.author?.headline)}</small>
                        <br><small class="text-muted">${escapeHtml(date)}</small>
                    </div>
                </div>
                <p class="card-text">${escapeHtml(post.content)}</p>
                <hr>
                <div class="d-flex gap-3">
                    <button class="btn ${likeButtonClass} btn-sm" id="like-btn-${post.id}"
                            data-liked="${isLiked}" onclick="toggleLike('${post.id}')">
                        <i class="bi ${likeIconClass} me-1" id="like-icon-${post.id}"></i><span id="likes-${post.id}">${post.likesCount || 0}</span>
                    </button>
                    <button class="btn btn-outline-secondary btn-sm" onclick="toggleComments('${post.id}')">
                        <i class="bi bi-chat me-1"></i><span id="comments-count-${post.id}">${post.commentsCount || 0}</span>
                    </button>
                </div>

                <!-- Section commentaires (cachée par défaut) -->
                <div id="comments-section-${post.id}" class="comments-section mt-3 d-none">
                    <hr>
                    <!-- Formulaire d'ajout de commentaire -->
                    <form class="mb-3" onsubmit="addComment(event, '${post.id}')">
                        <div class="d-flex gap-2">
                            <input type="text" class="form-control form-control-sm"
                                   id="comment-input-${post.id}"
                                   placeholder="Ecrire un commentaire..." required>
                            <button type="submit" class="btn btn-primary btn-sm">
                                <i class="bi bi-send"></i>
                            </button>
                        </div>
                    </form>
                    <!-- Liste des commentaires -->
                    <div id="comments-list-${post.id}">
                        <div class="text-center text-muted small">
                            <i class="bi bi-arrow-repeat spin"></i> Chargement...
                        </div>
                    </div>
                </div>
            </div>
        </div>`;
}

async function handleCreatePost(e) {
    e.preventDefault();

    const content = document.getElementById('post-content').value;

    try {
        const response = await apiRequest('/api/posts', {
            method: 'POST',
            body: JSON.stringify({ content })
        });

        if (response && response.ok) {
            document.getElementById('post-content').value = '';
            loadFeed();
        }
    } catch (error) {
        alert('Erreur lors de la publication');
    }
}

async function toggleLike(postId) {
    const btn = document.getElementById(`like-btn-${postId}`);
    const icon = document.getElementById(`like-icon-${postId}`);
    const likesEl = document.getElementById(`likes-${postId}`);

    if (!btn || !icon || !likesEl) return;

    const isCurrentlyLiked = btn.dataset.liked === 'true';
    const endpoint = isCurrentlyLiked ? 'unlike' : 'like';

    // Désactiver le bouton pendant la requête
    btn.disabled = true;

    try {
        const response = await apiRequest(`/api/posts/${postId}/${endpoint}`, { method: 'POST' });

        if (response && response.ok) {
            const result = await response.json();

            if (result.success) {
                // Mettre à jour l'état
                const newLikedState = !isCurrentlyLiked;
                btn.dataset.liked = newLikedState.toString();

                // Mettre à jour le compteur
                const currentCount = parseInt(likesEl.textContent);
                likesEl.textContent = newLikedState ? currentCount + 1 : currentCount - 1;

                // Mettre à jour l'apparence du bouton
                if (newLikedState) {
                    btn.classList.remove('btn-outline-primary');
                    btn.classList.add('btn-primary');
                    icon.classList.remove('bi-hand-thumbs-up');
                    icon.classList.add('bi-hand-thumbs-up-fill');
                } else {
                    btn.classList.remove('btn-primary');
                    btn.classList.add('btn-outline-primary');
                    icon.classList.remove('bi-hand-thumbs-up-fill');
                    icon.classList.add('bi-hand-thumbs-up');
                }
            }
        }
    } catch (error) {
        console.error('Erreur like/unlike:', error);
    } finally {
        btn.disabled = false;
    }
}

// Toggle affichage des commentaires
async function toggleComments(postId) {
    const section = document.getElementById(`comments-section-${postId}`);

    if (section.classList.contains('d-none')) {
        section.classList.remove('d-none');
        await loadComments(postId);
    } else {
        section.classList.add('d-none');
    }
}

// Charger les commentaires d'un post
async function loadComments(postId) {
    const container = document.getElementById(`comments-list-${postId}`);

    try {
        const response = await apiRequest(`/api/posts/${postId}/comments`);
        if (!response) return;

        const comments = await response.json();

        if (comments.length === 0) {
            container.innerHTML = '<p class="text-muted small">Aucun commentaire</p>';
            return;
        }

        container.innerHTML = comments.map(comment => createCommentHtml(comment)).join('');
    } catch (error) {
        container.innerHTML = '<p class="text-danger small">Erreur de chargement</p>';
    }
}

// Créer le HTML d'un commentaire
function createCommentHtml(comment) {
    const date = formatDate(comment.createdAt);
    const { user } = getAuthData();
    const isOwner = user && comment.author?.id === user.id;

    return `
        <div class="d-flex gap-2 mb-2 comment-item" id="comment-${comment.id}">
            <img src="${getAvatarUrl(comment.author?.avatarUrl, 32)}"
                 class="rounded-circle" width="32" height="32" alt="Avatar">
            <div class="flex-grow-1">
                <div class="bg-light rounded p-2">
                    <strong class="small">${escapeHtml(comment.author?.firstName)} ${escapeHtml(comment.author?.lastName)}</strong>
                    <p class="mb-0 small">${escapeHtml(comment.content)}</p>
                </div>
                <small class="text-muted">${escapeHtml(date)}</small>
                ${isOwner ? `<button class="btn btn-link btn-sm text-danger p-0 ms-2" onclick="deleteComment('${comment.id}', '${comment.postId}')">
                    <i class="bi bi-trash"></i>
                </button>` : ''}
            </div>
        </div>`;
}

// Ajouter un commentaire
async function addComment(e, postId) {
    e.preventDefault();

    const input = document.getElementById(`comment-input-${postId}`);
    const content = input.value.trim();

    if (!content) return;

    try {
        const response = await apiRequest(`/api/posts/${postId}/comments`, {
            method: 'POST',
            body: JSON.stringify({ content })
        });

        if (response && response.ok) {
            input.value = '';
            // Recharger les commentaires
            await loadComments(postId);
            // Mettre à jour le compteur
            const countEl = document.getElementById(`comments-count-${postId}`);
            if (countEl) {
                countEl.textContent = parseInt(countEl.textContent) + 1;
            }
        }
    } catch (error) {
        console.error('Erreur ajout commentaire:', error);
    }
}

// Supprimer un commentaire
async function deleteComment(commentId, postId) {
    if (!confirm('Supprimer ce commentaire ?')) return;

    try {
        const response = await apiRequest(`/api/comments/${commentId}`, {
            method: 'DELETE'
        });

        if (response && response.ok) {
            // Supprimer l'élément du DOM
            const commentEl = document.getElementById(`comment-${commentId}`);
            if (commentEl) {
                commentEl.remove();
            }
            // Mettre à jour le compteur
            const countEl = document.getElementById(`comments-count-${postId}`);
            if (countEl) {
                const count = parseInt(countEl.textContent) - 1;
                countEl.textContent = Math.max(0, count);
            }
        }
    } catch (error) {
        console.error('Erreur suppression commentaire:', error);
    }
}
