using System.Collections;
using System.Collections.Generic;
using TileManagement;
using Tiles;
using UnityEngine;

public class FoamNode : SpreadNode
{

	public const float MaxTimePresentFoam = 10f;
	public FoamSourceReservoir FoamSourceReservoir => SourceReservoir as FoamSourceReservoir;

	public override void Update()
	{
		//TODO apply reagents to people and stuff??
		//but lag?
		//but Applying to items
		//not yet

		PresentTimeCount += 1;
		if (PresentTimeCount > MaxTimePresentFoam)
		{
			if (FoamSourceReservoir.WallFoam)
			{
				if (FoamSourceReservoir.SmartFoam)
				{
					if (SourceReservoir.EdgeTiles.Contains(this))
					{
						OnMetaDataNode.PositionMatrix.MetaTileMap.SetTile(OnMetaDataNode.Position,
							SmokeAndFoamManager.Instance.WallFoam);
					}
				}
				else
				{
					OnMetaDataNode.PositionMatrix.MetaTileMap.SetTile(OnMetaDataNode.Position,
						SmokeAndFoamManager.Instance.WallFoam);
				}

				var tile = OnMetaDataNode.PositionMatrix.MetaTileMap.GetTile(OnMetaDataNode.Position, LayerType.Base);
				if (tile == null || ((BasicTile) tile).IsSpace())
				{
					OnMetaDataNode.PositionMatrix.MetaTileMap.SetTile(OnMetaDataNode.Position,
						SmokeAndFoamManager.Instance.BaseFoam);
				}
			}

			OnMetaDataNode.PositionMatrix.MetaTileMap.RemoveOverlaysOfType(OnMetaDataNode.Position, LayerType.Floors,OverlayType.Foam);
			SourceReservoir.RemoveTile(this);
		}
	}


	public override bool CheckIsEdge()
	{
		bool hasEmptyNeighbour = false;
		var worldPos = OnMetaDataNode.Position.ToWorld(OnMetaDataNode.PositionMatrix);

		foreach (var dir in dirs) //Might be lag
		{
			var newWorldPos = worldPos + dir;
			var matrix = MatrixManager.AtPoint(worldPos + dir, CustomNetworkManager.IsServer);

			MetaDataNode node = null;
			if (MatrixManager.Instance.spaceMatrix.MatrixInfo != matrix)
			{
				var newLocal = newWorldPos.ToLocal(matrix);
				node =  matrix.MetaDataLayer.Get(newLocal.RoundToInt());
			}
			else
			{
				var newLocal = newWorldPos.ToLocal(OnMetaDataNode.PositionMatrix);
				node = OnMetaDataNode.PositionMatrix.MetaDataLayer.Get(newLocal.RoundToInt());
			}

			if (node.FoamNode.IsActive == false || node.FoamNode.SourceReservoir != SourceReservoir)
			{
				hasEmptyNeighbour = true;
			}
		}

		return hasEmptyNeighbour;
	}

	public override void TrySpread()
	{
		var worldPos = OnMetaDataNode.Position.ToWorld(OnMetaDataNode.PositionMatrix);

		foreach (var dir in dirs) //Might be lag
		{
			if (SourceReservoir.StacksLeft == 0) break;

			var newWorldPos = worldPos + dir;
			var matrix = MatrixManager.AtPoint(worldPos + dir, CustomNetworkManager.IsServer);

			MetaDataNode node = null;
			if (MatrixManager.Instance.spaceMatrix.MatrixInfo != matrix)
			{
				var newLocal = newWorldPos.ToLocal(matrix);
				node =  matrix.MetaDataLayer.Get(newLocal.RoundToInt());
			}
			else
			{
				var newLocal = newWorldPos.ToLocal(OnMetaDataNode.PositionMatrix);
				node = OnMetaDataNode.PositionMatrix.MetaDataLayer.Get(newLocal.RoundToInt());
			}

			if (node.FoamNode.IsActive == false && node.IsOccupied == false)
			{
				SourceReservoir.SpreadToNode(this ,node.FoamNode);
			}
		}
	}

	public override void Clean()
	{
		base.Clean();
		OnMetaDataNode.IsSlippery = false;
		OnMetaDataNode.ForceUpdateClient();
	}


	public override void DistributeToTile(SourceReservoir sourceReservoir)
	{
		base.DistributeToTile(sourceReservoir);

		OnMetaDataNode.IsSlippery = true;
		OnMetaDataNode.ForceUpdateClient();

		var colour = Present.MixColor;
		if (Present.MixColor == Color.clear)
		{
			colour = Color.white;
		}

		OnMetaDataNode.PositionMatrix.MetaTileMap.AddOverlay(OnMetaDataNode.Position, SmokeAndFoamManager.Instance.OverlayTileFoam, Matrix4x4.identity, colour);
	}
}
public class FoamSourceReservoir : SourceReservoir
{
	public bool SmartFoam = false;
	public bool WallFoam = false;

	public override void RemoveTileInherit()
	{

	}
}