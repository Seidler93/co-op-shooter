public interface ILethalDamageHandler
{
    bool TryHandleLethalDamage(Health health, int incomingDamage, ulong attackerClientId);
}
