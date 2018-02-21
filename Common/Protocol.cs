/*
*       o__ __o       o__ __o__/_   o          o    o__ __o__/_   o__ __o                o    ____o__ __o____   o__ __o__/_   o__ __o      
*      /v     v\     <|    v       <|\        <|>  <|    v       <|     v\              <|>    /   \   /   \   <|    v       <|     v\     
*     />       <\    < >           / \o      / \  < >           / \     <\             / \         \o/        < >           / \     <\    
*   o/                |            \o/ v\     \o/   |            \o/     o/           o/   \o        |          |            \o/       \o  
*  <|       _\__o__   o__/_         |   <\     |    o__/_         |__  _<|           <|__ __|>      < >         o__/_         |         |> 
*   \          |     |            / \    \o  / \   |             |       \          /       \       |          |            / \       //  
*     \         /    <o>           \o/     v\ \o/  <o>           <o>       \o      o/         \o     o         <o>           \o/      /    
*      o       o      |             |       <\ |    |             |         v\    /v           v\   <|          |             |      o     
*      <\__ __/>     / \  _\o__/_  / \        < \  / \  _\o__/_  / \         <\  />             <\  / \        / \  _\o__/_  / \  __/>     
*
* THIS FILE IS GENERATED BY structgen.py/structs.yaml
* DO NOT EDIT
*
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WebArena {
	public enum Weapon {
		Gauntlet, 
		MachineGun, 
		Shotgun, 
		GrenadeLauncher, 
		RocketLauncher, 
		LightningGun, 
		Railgun, 
		PlasmaGun, 
		BFG
	}

	public enum PowerUp {
		Flight, 
		BattleSuit, 
		PersonalTeleporter, 
		PersonalMedkit, 
		QuadDamage, 
		Haste, 
		FiveHealth
	}

	public struct WeaponSpawn {
		public Weapon Weapon;
		public Vec3 Position;

		public WeaponSpawn(byte[] data, int offset = 0) : this() {
			Unpack(data, offset);
		}
		public WeaponSpawn(BinaryReader br) : this() {
			Unpack(br);
		}
		public void Unpack(byte[] data, int offset = 0) {
			using(var ms = new MemoryStream(data, offset, data.Length - offset)) {
				using(var br = new BinaryReader(ms)) {
					Unpack(br);
				}
			}
		}
		public void Unpack(BinaryReader br) {
			Weapon = (Weapon) br.ReadUInt32();
			Position = br.ReadVec3();
		}

		public byte[] Pack() {
			using(var ms = new MemoryStream()) {
				using(var bw = new BinaryWriter(ms)) {
					Pack(bw);
					return ms.ToArray();
				}
			}
		}
		public void Pack(BinaryWriter bw) {
			bw.Write((uint) Weapon);
			bw.Write(Position);
		}

		public override string ToString() => $"WeaponSpawn {{ Weapon={Weapon}, Position={Position} }}";
	}

	public struct MapChange {
		public string Name;
		public string Path;

		public MapChange(byte[] data, int offset = 0) : this() {
			Unpack(data, offset);
		}
		public MapChange(BinaryReader br) : this() {
			Unpack(br);
		}
		public void Unpack(byte[] data, int offset = 0) {
			using(var ms = new MemoryStream(data, offset, data.Length - offset)) {
				using(var br = new BinaryReader(ms)) {
					Unpack(br);
				}
			}
		}
		public void Unpack(BinaryReader br) {
			Name = br.ReadWaString();
			Path = br.ReadWaString();
		}

		public byte[] Pack() {
			using(var ms = new MemoryStream()) {
				using(var bw = new BinaryWriter(ms)) {
					Pack(bw);
					return ms.ToArray();
				}
			}
		}
		public void Pack(BinaryWriter bw) {
			bw.WaWrite(Name);
			bw.WaWrite(Path);
		}

		public override string ToString() => $"MapChange {{ Name={Name}, Path={Path} }}";
	}

	public abstract class ProtocolHandler {
		protected abstract void Send(byte[] data);
		public void Handle(object msg) {
			if(msg is WeaponSpawn) Handle((WeaponSpawn) msg);
			else if(msg is MapChange) Handle((MapChange) msg);
		}
		protected virtual void Handle(WeaponSpawn msg) { throw new NotImplementedException(); }
		protected virtual void Handle(MapChange msg) { throw new NotImplementedException(); }
		protected object ParseMessage(byte[] data) {
			var br = new BinaryReader(new MemoryStream(data));
			switch(br.ReadUInt32()) {
				case 0:
					return new WeaponSpawn(br);
				case 1:
					return new MapChange(br);
			}
			return null;
		}
		
		protected void SendMessage(WeaponSpawn value) {
			using(var ms = new MemoryStream()) {
				using(var bw = new BinaryWriter(ms)) {
					bw.Write(0U);
					value.Pack(bw);
				}
				Send(ms.ToArray());
			}
		}
		
		protected void SendMessage(MapChange value) {
			using(var ms = new MemoryStream()) {
				using(var bw = new BinaryWriter(ms)) {
					bw.Write(1U);
					value.Pack(bw);
				}
				Send(ms.ToArray());
			}
		}
	}
	
	public abstract class AsyncProtocolHandler {
		protected abstract Task Send(byte[] data);
		public async Task Handle(object msg) {
			if(msg is WeaponSpawn) await Handle((WeaponSpawn) msg);
			else if(msg is MapChange) await Handle((MapChange) msg);
		}
		protected virtual Task Handle(WeaponSpawn msg) { throw new NotImplementedException(); }
		protected virtual Task Handle(MapChange msg) { throw new NotImplementedException(); }
		protected object ParseMessage(byte[] data) {
			var br = new BinaryReader(new MemoryStream(data));
			switch(br.ReadUInt32()) {
				case 0:
					return new WeaponSpawn(br);
				case 1:
					return new MapChange(br);
			}
			return null;
		}
		
		protected async Task SendMessage(WeaponSpawn value) {
			using(var ms = new MemoryStream()) {
				using(var bw = new BinaryWriter(ms)) {
					bw.Write(0U);
					value.Pack(bw);
				}
				await Send(ms.ToArray());
			}
		}
		
		protected async Task SendMessage(MapChange value) {
			using(var ms = new MemoryStream()) {
				using(var bw = new BinaryWriter(ms)) {
					bw.Write(1U);
					value.Pack(bw);
				}
				await Send(ms.ToArray());
			}
		}
	}
}
