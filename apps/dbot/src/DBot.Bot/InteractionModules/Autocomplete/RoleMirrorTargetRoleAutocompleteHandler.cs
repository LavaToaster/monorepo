using DBot.Bot.Services;
using Discord;
using Discord.Interactions;

namespace DBot.Bot.InteractionModules.Autocomplete;

public class RoleMirrorTargetRoleAutocompleteHandler(RoleMirrorService service) : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
        IParameterInfo parameter, IServiceProvider services)
    {
        var guild = context.Guild;
        var results = new List<AutocompleteResult>();
        
        var candidateRoles = await service.GetAllRoleCandidatesAsync(guild.Id);

        foreach (var candidateRole in candidateRoles)
        {
            var role = await guild.GetRoleAsync(candidateRole.RoleId);
            
            results.Add(new AutocompleteResult(
                $"{guild.Name} - {role.Name}",
                $"{candidateRole.Id}"));
        }
  
        return AutocompletionResult.FromSuccess(results.Take(25));
    }
}