﻿using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityClient
{
    public class Main : BaseScript {
        public Main() {
            EventHandlers["onClientResourceStart"] += new Action<string>( OnClientResourceStart );
            Debug.WriteLine( "Hello world!" );
        }

        private void OnClientResourceStart( string resourceName ) {
            if( GetCurrentResourceName() != resourceName ) return;

            RegisterCommand( "car", new Action<int, List<object>, string>( ( source, args, raw ) => {
                // TODO: make a vehicle! fun!
                TriggerEvent( "chat:addMessage", new {
                    color = new[] { 255, 0, 0 },
                    args = new[] { "[CarSpawner]", $"I wish I could spawn this {(args.Count > 0 ? $"{args[0]} or" : "")} adder but my owner was too lazy. :(" }
                } );
            } ), false );
        }
    }
}
