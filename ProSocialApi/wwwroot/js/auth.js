// Gestion de l'authentification et de la navigation
document.addEventListener('DOMContentLoaded', function() {
    updateAuthNav();
});

function updateAuthNav() {
    const authNav = document.getElementById('auth-nav');
    if (!authNav) return;

    const token = localStorage.getItem('token');
    const user = JSON.parse(localStorage.getItem('user') || 'null');

    if (token && user) {
        authNav.innerHTML = `
            <li class="nav-item dropdown">
                <a class="nav-link dropdown-toggle" href="#" id="userDropdown" role="button"
                   data-bs-toggle="dropdown" aria-expanded="false">
                    <img src="${user.avatarUrl || 'https://imgs.search.brave.com/_57W_jU0seMHdPqYbm9r8JayXIdvU0fdbk8cRzww_8c/rs:fit:860:0:0:0/g:ce/aHR0cHM6Ly9tZWRp/YS5nZXR0eWltYWdl/cy5jb20vaWQvMTU3/NjE5OTA5L2ZyL3Bo/b3RvL2xhcGlucy10/ZW5hbnQtdW5lLWJh/bm5pJUMzJUE4cmUu/anBnP3M9NjEyeDYx/MiZ3PTAmaz0yMCZj/PVZzSUdVZ3dBTldR/OFFYT21HMjNEcGFC/M2lvWktPc1FFaGdw/VDBGaFMtMTg9'}"
                         class="rounded-circle me-1" width="32" height="32" alt="Avatar">
                    ${user.firstName}
                </a>
                <ul class="dropdown-menu dropdown-menu-end" aria-labelledby="userDropdown">
                    <li><a class="dropdown-item" href="/Home/Profile"><i class="bi bi-person me-2"></i>Mon profil</a></li>
                    <li><hr class="dropdown-divider"></li>
                    <li><a class="dropdown-item" href="/AuthView/Login" onclick="logout()"><i class="bi bi-box-arrow-right me-2"></i>Deconnexion</a></li>
                </ul>
            </li>`;
    } else {
        authNav.innerHTML = `
            <li class="nav-item">
                <a class="nav-link" href="/AuthView/Login"><i class="bi bi-box-arrow-in-right me-1"></i>Connexion</a>
            </li>
            <li class="nav-item">
                <a class="nav-link" href="/AuthView/Register"><i class="bi bi-person-plus me-1"></i>Inscription</a>
            </li>`;
    }
}

function logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.location.href = '/AuthView/Login';
}
