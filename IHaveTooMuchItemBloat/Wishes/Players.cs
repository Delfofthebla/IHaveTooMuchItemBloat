using RoR2;
using UnityEngine;

namespace IHaveTooMuchItemBloat.Wishes;

internal static class Players
{
    public static CharacterMaster MasterOf(Interactor interactor) => MasterOfBody(interactor ? interactor.gameObject : null);

    public static CharacterMaster MasterOfBody(GameObject bodyObject)
    {
        if (!bodyObject || !bodyObject.TryGetComponent(out CharacterBody body))
            return null;

        return body.master;
    }

    public static NetworkUser NetworkUserOf(CharacterMaster master)
    {
        var playerController = master ? master.playerCharacterMasterController : null;

        return playerController ? playerController.networkUser : null;
    }

    public static string NameOf(CharacterMaster master)
    {
        var networkUser = NetworkUserOf(master);
        if (networkUser)
            return networkUser.userName;

        return master ? master.name : "?";
    }
}
