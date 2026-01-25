using UnityEngine;

public abstract class BaseUtility : MonoBehaviour
{
    public Transform spawnPoint;
    public float cooldown = 0.1f;
    public int maxUses = 3;

    [SerializeField] protected int _currentUses;
    private float _lastUseTime;

    protected virtual void Awake()
    {
        _currentUses = maxUses;
    }

    public bool CanUse()
    {
        return Time.time >= _lastUseTime + cooldown && _currentUses > 0;
    }

    public virtual bool TryUse()
    {
        if (!CanUse())
        {
            OnUsageFailed();
            return false;
        }

        _lastUseTime = Time.time;
        _currentUses--;
        ExecuteUtility();
        return true;
    }

    protected abstract void ExecuteUtility();
    protected virtual void OnUsageFailed() { }

    public int CurrentUses => _currentUses;
    public int MaxUses => maxUses;
    public float CooldownRemaining => Mathf.Max(0, (_lastUseTime + cooldown) - Time.time);
    public bool IsOnCooldown => Time.time < _lastUseTime + cooldown;
    public abstract string UtilityName { get; }
}