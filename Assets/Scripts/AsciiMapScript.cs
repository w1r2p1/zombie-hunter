﻿using UnityEngine;
using System.Collections;
using AssemblyCSharp;
using System.Text;
using System;

public class AsciiMapScript : MonoBehaviour
{

	public float OriginX;
	public float OriginY;
	public float characterWidth;
	public float characterHeight;

	public GameObject prefabParent;
	public GameObject prefabMapBlockView;
	public String mapDataPath = "Assets/Maps/Test/";

	protected MapBlockData[,] mapDataGroup = new MapBlockData[3,3];
	protected MapFile mapfile;
	public int Worldx = 3;
	public int Worldy = 3;
	protected int MapCols = 20;
	protected int MapRows = 20;
	protected GameObject player;
	protected ArrayList lfj = new ArrayList ();

	String getMapPath(int x, int y) {
		StringBuilder sb = new StringBuilder (mapDataPath);
		sb.Append ("map");
		sb.Append (x);
		sb.Append ("x");
		sb.Append (y);
		sb.Append(".txt");
		return sb.ToString ();
	}

	String getWorldName(int x, int y) {
		return getObjectName ("World", x, y);
	}

	String getObjectName(String name, int x, int y) {
		StringBuilder sb = new StringBuilder (name);;
		sb.Append (x);
		sb.Append ("x");
		sb.Append (y);
		return sb.ToString ();
	}

	// Use this for initialization
	void Start ()
	{
		mapfile = new MapFile();

		if (prefabParent == null) {
			prefabParent = GameObject.Find ("AsciiMapCharacters");
		}

		if (prefabMapBlockView == null) {
			prefabMapBlockView = (GameObject)Resources.Load ("Main/MapBlockView", typeof(GameObject));
		}

		LoadMap (Worldx-1, Worldy-1, -1+1, -1+1,YieldDirection.NoYield);
		LoadMap (Worldx-1, Worldy, -1+1, 0+1,YieldDirection.NoYield);
		LoadMap (Worldx-1, Worldy+1, -1+1, 1+1,YieldDirection.NoYield);
		LoadMap (Worldx, Worldy-1, 0+1, -1+1,YieldDirection.NoYield);

		LoadMap (Worldx, Worldy+1, 0+1, 1+1,YieldDirection.NoYield);
		LoadMap (Worldx+1, Worldy-1, 1+1, -1+1,YieldDirection.NoYield);
		LoadMap (Worldx+1, Worldy, 1+1, 0+1,YieldDirection.NoYield);
		LoadMap (Worldx+1, Worldy+1, 1+1, 1+1,YieldDirection.NoYield);

		LoadMap (Worldx, Worldy, 0+1, 0+1,YieldDirection.NoYield);

		player = GameObject.Find ("Player");
		if (player == null) {
			throw new MissingReferenceException ("Player gameobject is missing");
		}

		// throw the player in the center of the map.
		Vector3 playerPosition = new Vector3(10 *characterWidth ,-10 * characterHeight,0) + calculateTransformPosition(Worldx, Worldy);
		player.transform.position = playerPosition;
	}

	void OnDisable() {
		SaveMap (Worldx - 1, Worldy - 1, -1 + 1, -1 + 1);
		SaveMap (Worldx-1, Worldy, -1+1, 0+1);
		SaveMap (Worldx-1, Worldy+1, -1+1, 1+1);
		SaveMap (Worldx, Worldy-1, 0+1, -1+1);

		SaveMap (Worldx, Worldy+1, 0+1, 1+1);
		SaveMap (Worldx+1, Worldy-1, 1+1, -1+1);
		SaveMap (Worldx+1, Worldy, 1+1, 0+1);
		SaveMap (Worldx+1, Worldy+1, 1+1, 1+1);

		SaveMap (Worldx, Worldy, 0+1, 0+1);
	}

	Vector3 calculateTransformPosition(int Worldx, int Worldy) {
		Vector3 retval;
		retval = new Vector3 ((MapRows * characterWidth * Worldx), (MapCols * characterHeight * -Worldy), 0);
		return retval;
	}

	void calculateXYPosition(Vector3 pos, ref int x, ref int y, int Worldx, int Worldy) {
		x = (int)((pos.x -(MapRows * characterWidth * Worldx) - OriginX)/characterWidth);
		y = (int)((pos.y - (MapCols * characterHeight * -Worldy) -OriginY) / -characterHeight);
	}




	void LoadMap(int Worldx, int Worldy, int x, int y, YieldDirection yieldDirection) {
		String mapPath = getMapPath (Worldx, Worldy);
		mapDataGroup[x, y] = mapfile.LoadFile (mapPath);
		StartCoroutine(InstantiateMap (mapDataGroup[x, y], Worldx, Worldy));
	}

	void LoadMapThreaded(int Worldx, int Worldy, int x, int y, YieldDirection yieldDirection) {
		String mapPath = getMapPath (Worldx, Worldy);
		LoadFileJob loadFileJob = new LoadFileJob ();
		loadFileJob.Worldx = Worldx;
		loadFileJob.Worldy = Worldy;
		loadFileJob.x = x;
		loadFileJob.y = y;
		loadFileJob.input = mapPath;
		loadFileJob.yieldDirection = yieldDirection;
		lfj.Add (loadFileJob);
		loadFileJob.Start ();
	}

	void SaveMap(int Worldx, int Worldy, int x, int y) {
		UnLoadMap (Worldx, Worldy, x, y);
		GameObject  world = GameObject.Find (getWorldName (Worldx, Worldy));
		if (world != null) {
			MapBlockView mapBlockView = world.GetComponent<MapBlockView> ();
			if (mapBlockView != null && mapBlockView.mapBlockData != null) {
				String mapPath = getMapPath (Worldx, Worldy);
				mapfile.SaveFile (mapBlockView.mapBlockData, mapPath);
			}
		}
	}

	void SaveMapThreaded(int Worldx, int Worldy, int x, int y) {
		UnLoadMap (Worldx, Worldy, x, y);
		GameObject  world = GameObject.Find (getWorldName (Worldx, Worldy));
		if (world != null) {
			MapBlockView mapBlockView = world.GetComponent<MapBlockView> ();
			if (mapBlockView != null && mapBlockView.mapBlockData != null) {
				String mapPath = getMapPath (Worldx, Worldy);
				SaveFileJob saveFileJob = new SaveFileJob ();
				saveFileJob.input = mapBlockView.mapBlockData;
				saveFileJob.path = mapPath;
				saveFileJob.Start ();
			}
		}
	}



	IEnumerator InstantiateMap(MapBlockData mapData, int Worldx, int Worldy) {
		return InstantiateMap(mapData,Worldx,Worldy, YieldDirection.NoYield);
}

	IEnumerator InstantiateMap(MapBlockData mapData, int Worldx, int Worldy, YieldDirection yieldDirection)
	{
		GameObject world = GameObject.Find (getWorldName (Worldx, Worldy));
		if (world == null) {
			world = (GameObject)Instantiate (prefabMapBlockView, new Vector3(0,0,0) , Quaternion.identity, prefabParent.transform);
			world.name = getWorldName (Worldx, Worldy);
			world.transform.position = calculateTransformPosition (Worldx, Worldy);

			MapBlockView mapBlockView = world.GetComponent<MapBlockView> ();
			if (mapBlockView == null) {
				throw new MissingComponentException ("Expected to find the MapBlockView Component");
			}
			mapBlockView.Initialize (Worldx, Worldy, mapData);
			yield return null;
		}
	}


	void UnLoadMap(int Worldx, int Worldy, int x, int y) {
		GameObject  world = GameObject.Find (getWorldName (Worldx, Worldy));
		if (world != null) {
	/*	Transform worldMainT = world.transform.FindChild ("worldMain");
		if (worldMainT != null) {
			for (int i = 0; i < worldMainT.childCount; i++) {
				Transform child = worldMainT.GetChild (i);
					int xobj = 0;
					int yobj = 0;
					String childName = child.name;
				int xindex = childName.IndexOf('x');
				if (xindex >= 0) {
					xobj = Int32.Parse (childName.Substring (3, xindex-3));
					yobj = Int32.Parse (childName.Substring (xindex+1));
					Vector3 pos = calculateTransformPosition (xobj, yobj, Worldx, Worldy);
					if (child.transform.position != pos) {
						int newXobj = 0;
						int newYobj = 0;
						calculateXYPosition (child.transform.position, ref newXobj, ref newYobj, Worldx, Worldy);
						// child has moved
						int newX = 1;
						int newY = 1;
						newX += (int)(newXobj / mapDataGroup [x, y].getRows ());
						newY += (int)(newYobj / mapDataGroup [x, y].getCols ());

						Debug.Log ("Child has moved");
						string newWorldName = getWorldName (Worldx + (int)(newXobj / mapDataGroup [x, y].getRows ()), Worldy + (int)(newYobj / mapDataGroup [x, y].getCols ()));
						Debug.Log ("NewWorldName " + newWorldName);
						GameObject  newWorld = GameObject.Find (newWorldName);
						Transform newWorldMainT = newWorld.transform.FindChild ("worldMain");
						child.transform.parent = newWorldMainT;

						newXobj = (int)(newXobj % mapDataGroup [x, y].getRows ());
						newYobj = (int)(newYobj % mapDataGroup [x, y].getCols ());
						if (newXobj < 0) {
							newXobj += mapDataGroup [x, y].getRows ();
						}
						if (newYobj < 0) {
							newYobj += mapDataGroup [x, y].getCols ();
						}
						Debug.Log("new XY ("+ newX + ", " + newY + ")");
						Debug.Log("new XYobj ("+ newXobj + ", " + newYobj + ")");
						Debug.Log("XY ("+ x + ", " + y + ")");
						Debug.Log("XYobj ("+ xobj + ", " + yobj + ")");
						child.name = getObjectName ("obj", newXobj, newYobj);
						mapDataGroup[newX, newY].setMainInt(newXobj, newYobj, mapDataGroup [x, y].getMainInt (xobj, yobj));
						mapDataGroup [x, y].setMainInt (xobj, yobj, 0);
					
					}
				}
			}
		}
*/

			DestroyObject (world);
			world = null;
		}
	}


	void FixedUpdate()
	{
		if (lfj != null) {
			foreach (LoadFileJob loadFileJob in lfj.ToArray()) {
				if (loadFileJob.Update ()) {
					lfj.Remove (loadFileJob);
					Debug.Log (loadFileJob.x);
					Debug.Log (loadFileJob.y);
					mapDataGroup[loadFileJob.x,loadFileJob.y] = loadFileJob.output;
					StartCoroutine (InstantiateMap (loadFileJob.output, loadFileJob.Worldx, loadFileJob.Worldy, loadFileJob.yieldDirection));
				}
			}
		}
		if (player != null) {
			Vector3 worldstart = calculateTransformPosition(Worldx, Worldy);
			Vector3 worldend = new Vector3(MapRows *characterWidth ,-MapCols * characterHeight,0) + calculateTransformPosition(Worldx, Worldy);
			if (player.transform.position.x < worldstart.x) {
				SaveMap(Worldx + 1, Worldy -1,  2, 0);
				SaveMap(Worldx + 1, Worldy,     2, 1);
				SaveMap(Worldx + 1, Worldy + 1, 2, 2);
				Worldx--;
				for (int x = 1; x >= 0; x--) {
					for (int y = 0; y < 3; y++) {
						mapDataGroup [x+1, y] = mapDataGroup [x, y];
					}
				}
				LoadMap (Worldx - 1, Worldy-1,  0, 0, YieldDirection.YieldLeft);
				LoadMap (Worldx - 1, Worldy,    0, 1, YieldDirection.YieldLeft);
				LoadMap (Worldx - 1, Worldy+1,  0, 2, YieldDirection.YieldLeft);
			}
			if (player.transform.position.x > worldend.x) {
				SaveMap(Worldx - 1, Worldy -1,  0, 0);
				SaveMap(Worldx - 1, Worldy,     0, 1);
				SaveMap(Worldx - 1, Worldy + 1, 0, 2);
				Worldx++;
				for (int x = 0; x <= 1; x++) {
					for (int y = 0; y < 3; y++) {
						mapDataGroup [x, y] = mapDataGroup [x+1, y];
					}
				}
				LoadMap (Worldx + 1, Worldy-1,  2, 0, YieldDirection.YieldRight);
				LoadMap (Worldx + 1, Worldy,    2, 1, YieldDirection.YieldRight);
				LoadMap (Worldx + 1, Worldy+1,  2, 2, YieldDirection.YieldRight);
			}
			if (player.transform.position.y > worldstart.y) {
				SaveMap(Worldx - 1, Worldy + 1, 0, 2);
				SaveMap(Worldx, Worldy +1    ,  1, 2);
				SaveMap(Worldx + 1, Worldy + 1, 2, 2);
				Worldy--;
				for (int y = 1; y >= 0; y--) {
					for (int x = 0; x < 3; x++) {
						mapDataGroup [x, y+1] = mapDataGroup [x, y];
					}
				}
				LoadMap (Worldx - 1, Worldy -1, 0, 0, YieldDirection.YieldUp);
				LoadMap (Worldx, Worldy - 1,    1, 0, YieldDirection.YieldUp);
				LoadMap (Worldx + 1, Worldy -1, 2, 0, YieldDirection.YieldUp);
			}
			if (player.transform.position.y < worldend.y) {
				SaveMap(Worldx - 1, Worldy - 1, 0, 0);
				SaveMap(Worldx, Worldy -1    ,  1, 0);
				SaveMap(Worldx + 1, Worldy - 1, 2, 0);
				Worldy++;
				for (int y = 0; y <= 1; y++) {
					for (int x = 0; x < 3; x++) {
						mapDataGroup [x, y] = mapDataGroup [x, y+1];
					}
				}
				LoadMap (Worldx - 1, Worldy +1, 0, 2, YieldDirection.YieldDown);
				LoadMap (Worldx, Worldy + 1,    1, 2, YieldDirection.YieldDown);
				LoadMap (Worldx + 1, Worldy +1, 2, 2, YieldDirection.YieldDown);
			}

		}
	}
}
