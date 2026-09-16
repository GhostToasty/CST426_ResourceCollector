using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/*
 * ResourceNode is a harvestable object like a tree or stone. Replicated health
 * counts down as players hit it with the right tool; at zero the server spawns
 * resource pickups and every client hides the depleted node.
 */

public class ResourceNode : Interactable
{
    [SerializeField] List<ObjectType> _toolTypeRequired = new();
    [SerializeField] NetworkObject _producedPrefab;
    [SerializeField] int _amountToSpawn = 3;
    [SerializeField] int _startingHealth = 1;
    [SerializeField] AudioClip _audioClip;

    readonly NetworkVariable<int> _health = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // TODO Slice 8.1: on the server, set health to _startingHealth.
        // NOTE: make sure  NetworkObject.Despawn(!NetworkObject.InScenePlaced); is done
        if (IsServer)
        _health.Value = _startingHealth;
        Debug.Log(_health.Value);
        // Next: Slice 8.2 CanInteract.

        // TODO Slice 8.5: subscribe to health changes and apply the current health.
        // Check: both windows hide a depleted tree. A late joiner sees it hidden.
        _health.OnValueChanged += HandleHealthChanged;
        HandleHealthChanged(_startingHealth, _health.Value);
        // Next: Slice 8.6 OnNetworkDespawn.
    }

    public override void OnNetworkDespawn()
    {
        // TODO Slice 8.6: unsubscribe from replicated health changes.
        _health.OnValueChanged -= HandleHealthChanged;
        // </> end of Slice 8
        // Next: Slice 9.1 in World/Receptacle.cs.
        
        base.OnNetworkDespawn();
    }

    public override bool CanInteract(ObjectType heldType)
    {
        // TODO Slice 8.2:
        // 1. Require a living node.
        // 2. Require an accepted tool.
        // Check: hold the axe. The tree highlights. Empty-handed, it does not.
        if (_health.Value > 0)
        {
            return _toolTypeRequired.Contains(heldType);
        }
        return false;
        // Next: Slice 8.3 Interact and HitFeedbackRpc.
    }

    protected override void Interact(PlayerHeldItem heldItem)
    {
        // TODO Slice 8.3:
        // 1. Reduce health.
        // 2. Call HitFeedbackRpc.
        // 3. Spawn _amountToSpawn copies of _producedPrefab with InstantiateAndSpawn.
        // 4. Place each with a small random XZ offset and random yaw.
        // Check: axe the tree. Wood appears. The mesh is still there until 8.4.
        _health.Value = _health.Value - 1;
        Debug.Log(_health.Value);
        HitFeedbackRpc();
        
        if (_health.Value <= 0)
        {
            int amount = _amountToSpawn;
            while (amount > 0)
            {
                NetworkObject.InstantiateAndSpawn(_producedPrefab.gameObject, NetworkManager, 
                    position: transform.position + new Vector3(Random.Range(-1, 1), 0, Random.Range(-2, 2)), 
                    rotation: transform.rotation * Quaternion.Euler(0, Random.Range(-15, 15), 0));
                amount--;
            }
        }
        // Next: Slice 8.4 HandleHealthChanged.
    }

    [Rpc(SendTo.ClientsAndHost)]
    void HitFeedbackRpc()
    {
        // TODO Slice 8.3: play the authored hit sound on each observer.
        AudioSource.PlayClipAtPoint(_audioClip, Vector3.zero);
    }

    void HandleHealthChanged(int previousValue, int newValue)
    {
        // TODO Slice 8.4: make the visuals and physics match the health.
        if (newValue <= 0)
            gameObject.SetActive(false);
        // Next: Slice 8.5 in OnNetworkSpawn — subscribe and apply.
    }
}
