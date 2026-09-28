<<<<<<< ours
// ADT: Закомментировано из-за использования генокрада от Goob Station
// using Content.Shared.Changeling.Components;
// using Content.Shared.Changeling.Systems;
// using Robust.Client.GameObjects;
||||||| base
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Robust.Client.GameObjects;
=======
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
>>>>>>> theirs

// namespace Content.Client.Changeling.Systems;

<<<<<<< ours
// public sealed class ChangelingIdentitySystem : SharedChangelingIdentitySystem
// {
//     [Dependency] private readonly UserInterfaceSystem _ui = default!;
||||||| base
public sealed class ChangelingIdentitySystem : SharedChangelingIdentitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
=======
public sealed partial class ChangelingIdentitySystem : SharedChangelingIdentitySystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;
>>>>>>> theirs

//     public override void Initialize()
//     {
//         base.Initialize();

<<<<<<< ours
//         SubscribeLocalEvent<ChangelingIdentityComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
//     }
||||||| base
        SubscribeLocalEvent<ChangelingIdentityComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }
=======
        SubscribeLocalEvent<ChangelingIdentityComponent, ComponentHandleState>(OnHandleState);
    }
>>>>>>> theirs

<<<<<<< ours
//     private void OnAfterAutoHandleState(Entity<ChangelingIdentityComponent> ent, ref AfterAutoHandleStateEvent args)
//     {
//         UpdateUi(ent);
//     }
||||||| base
    private void OnAfterAutoHandleState(Entity<ChangelingIdentityComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateUi(ent);
    }
=======
    private void OnHandleState(Entity<ChangelingIdentityComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not ChangelingIdentityComponentState state)
            return;

        ent.Comp.ConsumedIdentities = new List<ChangelingIdentityData>();

        foreach (var identity in state.ConsumedIdentities)
        {
            ChangelingIdentityData data = new()
            {
                Identity = EnsureEntity<ChangelingIdentityComponent>(identity.Identity, ent),
                Original = EnsureEntity<ChangelingIdentityComponent>(identity.Original, ent),
                OriginalMind = null, // Don't network the mind!
                OriginalJob = identity.OriginalJob,
                OriginalName = identity.OriginalName,
                Starting = identity.Starting,
                GrantedDna = identity.GrantedDna,
            };

            ent.Comp.ConsumedIdentities.Add(data);
        }

        ent.Comp.CurrentIdentity = EnsureEntity<ChangelingStoredIdentityComponent>(state.CurrentIdentity, ent);

        ent.Comp.IdentityCloningSettings = state.IdentityCloningSettings;
        ent.Comp.MaxStoredDisguises = state.MaxStoredDisguises;

        UpdateUi(ent);
    }
>>>>>>> theirs

//     public void UpdateUi(EntityUid uid)
//     {
//         if (_ui.TryGetOpenUi(uid, ChangelingTransformUiKey.Key, out var bui))
//         {
//             bui.Update();
//         }
//     }
// }
