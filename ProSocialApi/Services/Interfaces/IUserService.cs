// IUSERSERVICE.CS - Interface du service de gestion des utilisateurs
// Définit le contrat pour les opérations sur les profils utilisateurs :
// - Consultation de profil
// - Mise à jour de profil
// - Recherche d'utilisateurs
// - Suppression de compte

using ProSocialApi.DTOs.Users;

namespace ProSocialApi.Services.Interfaces;

/// <summary>
/// Interface pour le service de gestion des utilisateurs.
/// Gère les opérations CRUD sur les profils (hors authentification).
///
/// Implémentation : UserService
/// Enregistrement DI : AddScoped&lt;IUserService, UserService&gt;()
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Récupère le profil complet d'un utilisateur par son ID.
    /// Inclut les statistiques (nombre de connexions, nombre de posts).
    /// </summary>
    /// <param name="id">ID de l'utilisateur recherché</param>
    /// <returns>
    /// UserDto avec le profil complet, ou null si l'utilisateur n'existe pas
    /// </returns>
    Task<UserDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Met à jour le profil d'un utilisateur.
    /// Utilise le pattern Partial Update : seuls les champs non-null sont modifiés.
    ///
    /// Note : L'ID de l'utilisateur à modifier doit correspondre à l'utilisateur
    /// authentifié (vérifié dans le controller).
    /// </summary>
    /// <param name="id">ID de l'utilisateur à modifier</param>
    /// <param name="updateDto">Champs à mettre à jour (les champs null sont ignorés)</param>
    /// <returns>
    /// UserDto avec le profil mis à jour, ou null si l'utilisateur n'existe pas
    /// </returns>
    Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto updateDto);

    /// <summary>
    /// Recherche des utilisateurs par nom ou email.
    /// Utile pour trouver des personnes à ajouter en connexion.
    ///
    /// La recherche est effectuée sur :
    /// - Prénom (contient la requête)
    /// - Nom (contient la requête)
    /// - Email (contient la requête)
    /// </summary>
    /// <param name="query">Terme de recherche</param>
    /// <returns>Liste des utilisateurs correspondants (peut être vide)</returns>
    Task<List<UserDto>> SearchAsync(string query);

    /// <summary>
    /// Supprime définitivement le compte d'un utilisateur.
    /// Cette action est irréversible et supprime également :
    /// - Tous les posts de l'utilisateur (cascade)
    /// - Tous les commentaires de l'utilisateur (cascade)
    /// - Toutes les connexions de l'utilisateur (cascade)
    /// - Tous les likes de l'utilisateur (cascade)
    ///
    /// Note : L'ID doit correspondre à l'utilisateur authentifié (vérifié dans le controller).
    /// </summary>
    /// <param name="id">ID de l'utilisateur à supprimer</param>
    /// <returns>True si supprimé, false si l'utilisateur n'existait pas</returns>
    Task<bool> DeleteAsync(Guid id);
}
