﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Friflo.Fliox.Engine.ECS.Sync;
using Friflo.Json.Fliox.Mapper;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable RedundantExplicitArrayCreation
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Global
// ReSharper disable once CheckNamespace
namespace Friflo.Fliox.Engine.ECS;

internal static class GameEntityExtensions
{
    internal static int ComponentCount (this GameEntity entity) {
        return entity.archetype.structCount + entity.Scripts.Length;
    }
}
    
    
internal static class GameEntityUtils
{
    internal static string GameEntityToString(GameEntity entity, StringBuilder sb)
    {
        var archetype = entity.archetype;
        sb.Append("id: ");
        sb.Append(entity.id);
        if (archetype == null) {
            sb.Append("  (detached)");
            return sb.ToString();
        }
        if (entity.HasName) {
            var name = entity.Name.value;
            if (name != null) {
                sb.Append("  \"");
                sb.Append(name);
                sb.Append('\"');
                return sb.ToString();
            }
        }
        if (entity.ComponentCount() == 0) {
            sb.Append("  []");
        } else {
            sb.Append("  [");
            var scripts = GetScripts(entity);
            foreach (var script in scripts) {
                sb.Append('*');
                sb.Append(script.GetType().Name);
                sb.Append(", ");
            }
            foreach (var heap in archetype.Heaps) {
                sb.Append(heap.StructType.Name);
                sb.Append(", ");
            }
            sb.Length -= 2;
            sb.Append(']');
        }
        return sb.ToString();
    }

    private static readonly EntityConverter DebugConverter       = new EntityConverter();
    private static readonly DataEntity      DebugDataEntity      = new DataEntity();      
    private static readonly ObjectWriter    DebugObjectWriter    = new ObjectWriter(new TypeStore()); // todo use global TypeStore
    
    internal static string GetDebugJSON(GameEntity entity)
    {
        lock (DebugConverter) {
            DebugConverter.GameToDataEntity(entity, DebugDataEntity, true);
            DebugObjectWriter.Pretty             = true;
            DebugObjectWriter.WriteNullMembers   = false;
            return DebugObjectWriter.Write(DebugDataEntity);
        }
    }
    
    internal static IComponent[] GetComponentsDebug(GameEntity entity)
    {
        var archetype   = entity.archetype;
        var count       = archetype.structCount;
        if (count == 0) {
            return EmptyComponents;
        }
        var components  = new IComponent[count];
        // --- add components
        var heaps       = archetype.Heaps;
        for (int n = 0; n < count; n++) {
            components[n] = heaps[n].GetComponentDebug(entity.compIndex); 
        }
        return components;
    }
    
    // ---------------------------------- Script utils ----------------------------------
    private  static readonly IComponent[]   EmptyComponents = Array.Empty<IComponent>();
    private  static readonly Script[]       EmptyScripts  = Array.Empty<Script>();
    internal const  int                     NoScripts     = -1;  
    
    private  static Exception MissingAttributeException(Type type) {
        var msg = $"Missing attribute [Script(\"<key>\")] on type: {type.Namespace}.{type.Name}";
        return new InvalidOperationException(msg);
    }

    internal static Script[] GetScripts(GameEntity entity) {
        if (entity.scriptIndex == NoScripts) {
            return EmptyScripts;
        }
        return entity.archetype.gameEntityStore.GetScripts(entity);
    }
    
    internal static Script GetScript(GameEntity entity, Type scriptType)
    {
        if (entity.scriptIndex == NoScripts) {
            return null;
        }
        return entity.archetype.gameEntityStore.GetScript(entity, scriptType);
    }
    
    internal static Script AddScript(GameEntity entity, Script script, Type scriptType, int classIndex)
    {
        if (classIndex == ClassUtils.MissingAttribute) {
            throw MissingAttributeException(scriptType);
        }
        if (script.entity != null) {
            throw new InvalidOperationException($"script already added to an entity. current entity id: {script.entity.id}");
        }
        return entity.archetype.gameEntityStore.AddScript(entity, script, scriptType);
    }
    
    internal static Script RemoveScript(GameEntity entity, Type scriptType) {
        if (entity.scriptIndex == NoScripts) {
            return null;
        }
        return entity.archetype.gameEntityStore.RemoveScript(entity, scriptType);
    }
}