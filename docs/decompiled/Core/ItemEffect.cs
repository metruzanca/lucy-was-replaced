using UnityEngine;

public class ItemEffect : MonoBehaviour
{
	[SerializeField]
	private MeshFilter meshFilter;

	public void Setup(int itemId)
	{
		ItemSO item = ResourceManager.GetItem(itemId);
		meshFilter.mesh = item.mesh;
	}
}
