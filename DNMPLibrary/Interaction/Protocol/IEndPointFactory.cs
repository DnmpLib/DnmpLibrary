﻿namespace DNMPLibrary.Interaction.Protocol
{
    public interface IEndPointFactory
    {
        byte[] SerializeEndPoint(IEndPoint endPoint);

        IEndPoint DeserializeEndPoint(byte[] endPoint);
    }
}
