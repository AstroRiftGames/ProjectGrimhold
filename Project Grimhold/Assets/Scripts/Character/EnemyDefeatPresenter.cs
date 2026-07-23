using UnityEngine;

/// <summary>
/// Presenter component responsible for procedural enemy defeat animation and visual pose.
/// Inherits core defeat presentation logic from <see cref="DefeatPresenterBase"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class EnemyDefeatPresenter : DefeatPresenterBase
{
    // The defeated enemy is also its corpse and must remain visible so future
    // death animation can settle into a persistent inspectable pose.
    protected override bool HideBodyVisualAfterTransition => false;
}
