using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamemodeCityServer {
    public class Vote : BaseScript {

        int initPlayerCount;
        int votes = 0;

        Action<object> winner;

        Dictionary<object, int> Votes = new Dictionary<object, int>();

        public Vote( Action<object> win ) {
            initPlayerCount = new PlayerList().Count();
            winner = win;
        }

        public void MakeVote( object ID ) {
            if( !Votes.ContainsKey( ID ) )
                Votes.Add( ID, 1 );
            Votes[ID]++;
            votes++;

            if( votes == initPlayerCount ) {
                winner( GetWinner() );
            }
        }

        public object GetWinner() {
            return Votes.Aggregate( ( l, r ) => l.Value > r.Value ? l : r ).Key;
        }

    }
}
