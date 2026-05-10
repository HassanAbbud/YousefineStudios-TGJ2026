using Unity.Netcode.Components;

namespace Networked
{
    /// <summary>
    /// Owner-authoritative NetworkTransform. The owning client decides where
    /// this object is, instead of the server pushing positions back at it.
    ///
    /// REQUIRED for first-person movement. With the default server-authoritative
    /// NetworkTransform, the client moves locally with CharacterController.Move()
    /// but the server keeps overwriting that position back, so the client appears
    /// frozen / can't move. This subclass flips that.
    ///
    /// USAGE: Replace any "NetworkTransform" component on the NetworkPlayer prefab
    /// with this one ("ClientNetworkTransform"). Same inspector, same fields.
    /// </summary>
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}