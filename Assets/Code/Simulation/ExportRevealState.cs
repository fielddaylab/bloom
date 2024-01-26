using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zavala.Data;
using Zavala.Sim;

namespace Zavala.World
{
    public class ExportRevealState : SharedStateComponent, IRegistrationCallbacks, ISaveStateChunkObject, ISaveStatePostLoad
    {
        [NonSerialized] public List<GameObject>[] ObstructionsPerRegion;
        [NonSerialized] public GameObject[] DepotsPerRegion;
        [NonSerialized] public BitSet32 DepotRevealMask;

        public void OnRegister()
        {
            ObstructionsPerRegion = new List<GameObject>[RegionInfo.MaxRegions];
            for (int i = 0; i <  RegionInfo.MaxRegions; i++)
            {
                ObstructionsPerRegion[i] = new List<GameObject>();
            }

            DepotsPerRegion = new GameObject[RegionInfo.MaxRegions];

            ZavalaGame.SaveBuffer.RegisterHandler("ExportDepots", this);
            ZavalaGame.SaveBuffer.RegisterPostLoad(this);
        }

        public void OnDeregister() {
            ZavalaGame.SaveBuffer.DeregisterHandler("ExportDepots");
            ZavalaGame.SaveBuffer.DeregisterPostLoad(this);
        }

        void ISaveStateChunkObject.Write(object self, ref ByteWriter writer, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            DepotRevealMask.Unpack(out uint bits);
            writer.Write((byte) bits);
        }

        void ISaveStateChunkObject.Read(object self, ref ByteReader reader, SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            DepotRevealMask = new BitSet32(reader.Read<byte>());
        }

        void ISaveStatePostLoad.PostLoad(SaveStateChunkConsts consts, ref SaveScratchpad scratch) {
            for(int i = 0; i < consts.MaxRegions; i++) {
                if (DepotRevealMask[i]) {
                    SimWorldUtility.RevealExportDepotInstantly(ZavalaGame.SimWorld, ZavalaGame.SimGrid, this, i);
                }
            }
        }
    }
}