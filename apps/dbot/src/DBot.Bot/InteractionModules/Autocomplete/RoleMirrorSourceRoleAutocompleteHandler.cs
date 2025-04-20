using DBot.Bot.Services;
using Discord;
using Discord.Interactions;

namespace DBot.Bot.InteractionModules.Autocomplete;

public class RoleMirrorSourceRoleAutocompleteHandler(RoleMirrorService service) : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter, IServiceProvider services)
    {
        var currentUser = context.User;
        var results = new List<AutocompleteResult>();
        var guilds = await context.Client.GetGuildsAsync();

        // I'm not expecting this to be in more than a handful of guilds, so I don't care about performance :)
        foreach (var guild in guilds)
        {
            if (guild.Id == context.Guild.Id)
            {
                // Skip the current guild
                continue;
            }
            
            // If the user is not in the guild, skip it
            if (await guild.GetUserAsync(currentUser.Id) == null) continue;
            
            var candidateRoles = await service.GetAllRoleCandidatesAsync(guild.Id);
            foreach (var candidateRole in candidateRoles)
            {
                var role = await guild.GetRoleAsync(candidateRole.RoleId);
                
                results.Add(new AutocompleteResult(
                    $"{guild.Name} - {role.Name}",
                    $"{candidateRole.Id}"));
            }
        }
            
        return AutocompletionResult.FromSuccess(results.Take(25));
    }
}

