namespace devanewbot.Services;

using System.Threading.Tasks;

/// <summary>
/// The fabled `brb3bot`
/// </summary>
public interface IWelcomeService
{
    /// <summary>
    /// When a user joins a channel, welcome them if it's their first time joining.
    /// </summary>
    /// <param name="channelId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task WelcomeNewUser(string channelId, string userId);
}
