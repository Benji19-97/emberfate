// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Events;
//
// namespace Runtime.WorkInProgress
// {
//     public class Actor : MonoBehaviour
//     {
//         public ActorStats ActorStats;
//         
//         public UnityEvent<Action> UseEvent;
//         public UnityEvent<Actor> PerSecondEvent;
//         public UnityEvent<Actor, Damage> DamageEvent;
//         public UnityEvent<Actor, Damage> TakeDamageEvent;
//
//         private HashSet<EffectHandler> EffectHandlers;
//
//         public TraitHolder TraitHolder = new TraitHolder();
//
//         private void Start()
//         {
//             RegisterHandlers();
//         }
//
//         private void RegisterHandlers()
//         {
//             TraitHolder.TraitsChangedEvent.AddListener(OnTraitsChanged);
//
//             UseEvent.AddListener(InvokeEffectHandlersOnUse);
//             PerSecondEvent.AddListener(InvokeEffectHandlersPerSecond);
//             DamageEvent.AddListener(InvokeEffectHandlersOnDamage);
//             TakeDamageEvent.AddListener(InvokeEffectHandlersOnTakeDamage);
//         }
//
//         private void InvokeEffectHandlersOnUse(Action action)
//         {
//             foreach (var effectHandler in EffectHandlers)
//             {
//                 effectHandler.OnUse(action);
//             }
//         }
//         
//         private void InvokeEffectHandlersPerSecond(Actor actor)
//         {
//             foreach (var effectHandler in EffectHandlers)
//             {
//                 effectHandler.PerSecond(actor);
//             }
//         }
//         
//         private void InvokeEffectHandlersOnDamage(Actor actor, Damage damage)
//         {
//             foreach (var effectHandler in EffectHandlers)
//             {
//                 effectHandler.OnDamage(actor, damage);
//             }
//         }
//         
//         private void InvokeEffectHandlersOnTakeDamage(Actor actor, Damage damage)
//         {
//             foreach (var effectHandler in EffectHandlers)
//             {
//                 effectHandler.OnTakeDamage(actor, damage);
//             }
//         }
//
//         private void OnTraitsChanged()
//         {
//             ActorStats.QueryStats();
//             RefreshEffectHandlers();
//         }
//
//         private void RefreshEffectHandlers()
//         {
//             EffectHandlers.Clear();
//
//             foreach (var trait in TraitHolder.GetAllTraits(TraitCategory.Effect))
//             {
//                 if (trait.EffectHandler != null && !EffectHandlers.Contains(trait.EffectHandler))
//                 {
//                     EffectHandlers.Add(trait.EffectHandler);
//                 }
//             }
//         }
//     }
// }