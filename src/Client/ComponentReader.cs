﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Fliox.Engine.ECS;
using Friflo.Json.Burst;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Fliox.Engine.Client;

/// <summary>
/// Create all class / struct components for an entity from <see cref="JsonValue"/> used as <see cref="DataNode.components"/>
/// </summary>
internal sealed class ComponentReader
{
    private readonly    ObjectReader                        componentReader;
    private readonly    Dictionary<string, ComponentType>   componentSchema;
    private readonly    List<ComponentType>                 structTypes;
    private readonly    ArchetypeKey                        searchKey;
    private             Utf8JsonParser                      parser;
    private             Bytes                               buffer;
    private             RawComponent[]                      components;
    private             int                                 componentCount;
    
    internal static readonly ComponentReader Instance = new ComponentReader();
    
    private ComponentReader() {
        buffer          = new Bytes(128);
        components      = new RawComponent[1];
        componentReader = new ObjectReader(EntityStore.Static.TypeStore);
        componentSchema = new Dictionary<string, ComponentType>(EntityStore.Static.ComponentSchema.ComponentTypeByKey);
        structTypes     = new List<ComponentType>();
        searchKey       = new ArchetypeKey();
    }
    
    internal void Read(JsonValue value, GameEntity entity, EntityStore store)
    {
        if (value.IsNull()) {
            return;
        }
        parser.InitParser(value);
        var ev = parser.NextEvent();
        if (ev == JsonEvent.ValueNull) {
            return;
        }
        if (ev != JsonEvent.ObjectStart) {
            throw new InvalidOperationException("expect object or null");
        }
        ReadRawComponents();
        SetEntityArchetype(entity, store);
        ReadComponents(entity);
    }
    
    private void ReadComponents(GameEntity entity)
    {
        for (int n = 0; n < componentCount; n++)
        {
            var component = components[n];
            buffer.Clear();
            parser.AppendInputSlice(ref buffer, component.start - 1, component.end);
            var json = new JsonValue(buffer);
            if (component.type.kind == ComponentKind.Class) {
                // --- read class component
                component.type.ReadClassComponent(componentReader, json, entity);
                continue;
            }
            // --- read struct component
            var structType  = component.type;
            var heap        = entity.archetype.heapMap[structType.structIndex];
            if (heap != null) {
                // --- change component value
                heap.Read(componentReader, entity.compIndex, json);
                continue;
            }
            var msg = $"unexpected: heap == null. structType: {structType}. {nameof(SetEntityArchetype)} ensures this.";
            throw new InvalidOperationException(msg);
        }
    }
    
    /// <summary>
    /// Ensures the given entity present / moved to an <see cref="Archetype"/> that contains all struct components 
    /// within the current JSON payload.
    /// </summary>
    private void SetEntityArchetype(GameEntity entity, EntityStore store)
    {
        searchKey.Clear();
        var count = componentCount;
        for (int n = 0; n < count; n++)
        {
            ref var component   = ref components[n];
            var type            = componentSchema[component.key];
            component.type      = type;
            searchKey.structs.bitSet.SetBit(type.structIndex);
        }
        searchKey.CalculateHashCode();
        
        // --- use / create Archetype with present components to eliminate structural changes for every individual component Read()
        var curArchetype = entity.archetype;
        Archetype newArchetype;
        if (store.archetypeSet.TryGetValue(searchKey, out var archetypeId)) {
            newArchetype = archetypeId.type;
        } else {
            var config = store.GetArchetypeConfig();
            structTypes.Clear();
            for (int n = 0; n < count; n++) {
                ref var component = ref components[n];
                if (component.type.kind == ComponentKind.Struct) {
                    structTypes.Add(component.type);
                }
            }
            newArchetype = Archetype.CreateWithStructTypes(config, structTypes, curArchetype.tags);
            store.AddArchetype(newArchetype);
        }
        if (curArchetype == newArchetype) {
            return;
        }
        if (curArchetype == store.defaultArchetype) {
            newArchetype.AddEntity(entity.id);
        } else {
            curArchetype.MoveEntityTo(entity.id, entity.compIndex, newArchetype);
        }
        entity.archetype = newArchetype;
    }
    
    private void ReadRawComponents()
    {
        componentCount = 0;
        var ev = parser.NextEvent();
        while (true) {
            switch (ev) {
                case JsonEvent.ObjectStart:
                    var key     = parser.key.AsString();  // todo remove string allocation
                    var start   = parser.Position;
                    parser.SkipTree();
                    if (componentCount == components.Length) {
                        Utils.Resize(ref components, 2 * componentCount);
                    }
                    components[componentCount++] = new RawComponent { key = key, start = start, end = parser.Position };
                    ev = parser.NextEvent();
                    if (ev == JsonEvent.ObjectEnd) {
                        return;
                    }
                    break;
                case JsonEvent.ObjectEnd:
                    return;
                default:
                    throw new InvalidOperationException($"expect object. was: {ev}");
            }
        }
    }
}

internal struct RawComponent
{
    internal        string          key;
    internal        ComponentType   type;
    internal        int             start;
    internal        int             end;

    public override string          ToString() => key;
}