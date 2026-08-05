using Tracker.TaskService.Services;

namespace Tracker.TaskService.Endpoints;

public static class AdminUserEndpoints
{
    public static void MapAdminUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin").RequireAuthorization("AdminOnly");

        group.MapGet("/users", async Task<IResult> (IKeycloakAdminClient keycloak) =>
        {
            try
            {
                return Results.Ok(await keycloak.GetUsersWithRolesAsync());
            }
            catch (KeycloakAdminException ex)
            {
                return KeycloakProblem(ex);
            }
        });

        group.MapGet("/roles", async Task<IResult> (IKeycloakAdminClient keycloak) =>
        {
            try
            {
                return Results.Ok(await keycloak.GetRealmRolesAsync());
            }
            catch (KeycloakAdminException ex)
            {
                return KeycloakProblem(ex);
            }
        });

        group.MapPost("/users/{userId}/roles/{roleName}", async Task<IResult> (string userId, string roleName, IKeycloakAdminClient keycloak) =>
        {
            try
            {
                await keycloak.AssignRealmRoleAsync(userId, roleName);
                return Results.NoContent();
            }
            catch (KeycloakAdminException ex)
            {
                return KeycloakProblem(ex);
            }
        });

        group.MapDelete("/users/{userId}/roles/{roleName}", async Task<IResult> (string userId, string roleName, IKeycloakAdminClient keycloak) =>
        {
            try
            {
                await keycloak.RemoveRealmRoleAsync(userId, roleName);
                return Results.NoContent();
            }
            catch (KeycloakAdminException ex)
            {
                return KeycloakProblem(ex);
            }
        });
    }

    private static IResult KeycloakProblem(KeycloakAdminException ex) =>
        Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status502BadGateway, title: "Keycloak Admin API error");
}
