namespace BattleHunter.Core.State.Actions;

/// <summary>Intenção enviada pelo cliente (ou pela IA). O Reducer valida; nunca o cliente.</summary>
public abstract record GameAction;

/// <summary>Intenção de um caçador específico.</summary>
public abstract record HunterAction(int HunterId) : GameAction;
