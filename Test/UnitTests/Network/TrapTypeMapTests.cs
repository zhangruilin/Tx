﻿
namespace Tests.Tx.Network
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Text;

    using global::Tx.Network;
    using global::Tx.Network.Snmp;
    using global::Tx.Network.Snmp.Dynamic;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TrapTypeMapTests
    {
        private UdpDatagram fakeTrapUdp;

        private IpPacket fakeIpPacket;

        [TestInitialize]
        public void TestInitialize()
        {
            var integerVarBind = new VarBind(
                new ObjectIdentifier("1.3.6.1.4.1.1.1.1"),
                5L,
                new Asn1TagInfo(Asn1Tag.Integer, ConstructType.Primitive, Asn1Class.Universal));

            var sysUpTime = new VarBind(new ObjectIdentifier("1.3.6.1.2.1.1.3.0"),
                506009u,
                new Asn1TagInfo(Asn1SnmpTag.TimeTicks));

            var trapVb = new VarBind(new ObjectIdentifier("1.3.6.1.6.3.1.1.4.1.0"),
                new ObjectIdentifier("1.3.6.1.4.1.500.12"),
                new Asn1TagInfo(Asn1Tag.ObjectIdentifier, ConstructType.Primitive, Asn1Class.Universal));

            var extraneousVb = new VarBind(new ObjectIdentifier("1.3.6.1.6.3.1.1.42.42.42.0"),
                8938ul,
                new Asn1TagInfo(Asn1SnmpTag.Counter64));

            var packet = new SnmpDatagram(
                PduType.SNMPv2Trap,
                SnmpVersion.V2C,
                "Community",
                50000,
                SnmpErrorStatus.NoError,
                0,
                new[] { sysUpTime, trapVb, integerVarBind, extraneousVb });

            var encoded = packet.ToSnmpEncodedByteArray();

            this.fakeIpPacket = new IpPacket(
                NetworkInterfaceComponent.IPv4,
                Byte.MaxValue,
                Byte.MaxValue,
                Byte.MaxValue,
                UInt16.MaxValue,
                UInt16.MaxValue,
                Byte.MaxValue,
                UInt16.MaxValue,
                Byte.MaxValue,
                ProtocolType.Udp,
                IPAddress.Parse("1.1.1.1"),
                IPAddress.Parse("2.2.2.2"),
                new byte[0],
                new byte[0]);

            this.fakeTrapUdp = new UdpDatagram(this.fakeIpPacket, 10, 10, (ushort)(encoded.Length + 8), encoded);
        }

        [TestMethod]
        public void TestUnattributedClassReturnsDefaultKey()
        {
            var typeMap = new TrapTypeMap();
            var key = typeMap.GetTypeKey(typeof(UnmarkedTrap));

            Assert.IsNotNull(key);
            Assert.AreEqual(default(ObjectIdentifier), key);
        }

        [TestMethod]
        public void TestUnattributedClassReturnsNullTransform()
        {
            var typeMap = new TrapTypeMap();
            var transform = typeMap.GetTransform(typeof(UnmarkedTrap));

            Assert.IsNull(transform);
        }

        [TestMethod]
        public void TestFakeTrapTransform()
        {
            var typeMap = new TrapTypeMap();

            var transform = typeMap.GetTransform(typeof(FakeTrap));

            Assert.IsNotNull(transform);

            var receivedTime = DateTimeOffset.UtcNow;
            this.fakeTrapUdp.ReceivedTime = receivedTime;
            var transformedOutput = transform(this.fakeTrapUdp) as FakeTrap;

            Assert.IsNotNull(transformedOutput);
            Assert.AreEqual(506009u, transformedOutput.SysUpTime);
            Assert.AreEqual(5L, transformedOutput.Integer);
            Assert.IsNotNull(transformedOutput.Objects);
            Assert.AreEqual(4, transformedOutput.Objects.Count);
            Assert.AreEqual(IPAddress.Parse("1.1.1.1"), transformedOutput.SourceAddress);
            Assert.AreEqual(receivedTime, transformedOutput.ReceivedTime);

            var varbinds = transformedOutput.Objects;
            var extraneous = varbinds.FirstOrDefault(vb => vb.Oid.Equals(new ObjectIdentifier("1.3.6.1.6.3.1.1.42.42.42.0")));

            Assert.IsNotNull(extraneous);
            Assert.AreNotEqual(default(VarBind), extraneous);
            Assert.AreEqual(8938ul, extraneous.Value);
        }

        [TestMethod]
        public void TestNullTrap()
        {
            var typeMap = new TrapTypeMap();

            var transform = typeMap.GetTransform(typeof(FakeTrap));

            Assert.IsNotNull(transform);

            var transformedOutput = transform(null) as FakeTrap;

            Assert.IsNull(transformedOutput);
        }

        [TestMethod]
        public void TestFakeTrapStringIpTransform()
        {
            var typeMap = new TrapTypeMap();

            var transform = typeMap.GetTransform(typeof(FakeTrapStringIp));

            Assert.IsNotNull(transform);

            var transformedOutput = transform(this.fakeTrapUdp) as FakeTrapStringIp;

            Assert.IsNotNull(transformedOutput);
            Assert.AreEqual("1.1.1.1", transformedOutput.SourceAddress);
        }

        [TestMethod]
        public void TestFakeTrapInputKey()
        {
            var typeMap = new TrapTypeMap();
            var inputKey = typeMap.GetInputKey(this.fakeTrapUdp);

            Assert.AreEqual(new ObjectIdentifier("1.3.6.1.4.1.500.12"), inputKey);

            var typeKey = typeMap.GetTypeKey(typeof(FakeTrap));

            Assert.AreEqual(inputKey, typeKey);
        }

        [TestMethod]
        public void Test_OctetStringAsByteArray_1()
        {
            var typeMap = new TrapTypeMap();

            var transform = typeMap.GetTransform(typeof(FakeTrap2));

            Assert.IsNotNull(transform);

            var octetStringVarBind = new VarBind(
                new ObjectIdentifier("1.3.6.1.4.1.562.29.6.2.2"),
                "Hello",
                new Asn1TagInfo(Asn1Tag.OctetString, ConstructType.Primitive, Asn1Class.Universal));

            var sysUpTime = new VarBind(new ObjectIdentifier("1.3.6.1.2.1.1.3.0"),
                506009u,
                new Asn1TagInfo(Asn1SnmpTag.TimeTicks));

            var trapVb = new VarBind(new ObjectIdentifier("1.3.6.1.6.3.1.1.4.1.0"),
                new ObjectIdentifier("1.3.6.1.4.1.500.12"),
                new Asn1TagInfo(Asn1Tag.ObjectIdentifier, ConstructType.Primitive, Asn1Class.Universal));

            var packet = new SnmpDatagram(
                PduType.SNMPv2Trap,
                SnmpVersion.V2C,
                "Community",
                50000,
                SnmpErrorStatus.NoError,
                0,
                new[] { sysUpTime, trapVb, octetStringVarBind });

            var encoded = packet.ToSnmpEncodedByteArray();

            var udpDatagram = new UdpDatagram(this.fakeIpPacket, 10, 10, (ushort)(encoded.Length + 8), encoded);

            var transformedOutput = transform(udpDatagram) as FakeTrap2;

            Assert.IsNotNull(transformedOutput);
            Assert.IsNotNull(transformedOutput.Property);
            Assert.AreEqual("Hello", Encoding.UTF8.GetString(transformedOutput.Property));
            Assert.AreEqual("Hello", transformedOutput.StringProperty);
        }

        [TestMethod]
        public void Test_OctetStringAsByteArray_2()
        {
            var typeMap = new TrapTypeMap();

            var transform = typeMap.GetTransform(typeof(FakeTrap2));

            Assert.IsNotNull(transform);

            var payload = new byte[] { 0x07, 0xE0, 0x06, 0x0E, 0x0E, 0x1E, 0x0E, 0x00 };

            var octetStringVarBind = new VarBind(
                new ObjectIdentifier("1.3.6.1.4.1.562.29.6.2.2"),
                payload.ReadOctetString(0, payload.Length),
                new Asn1TagInfo(Asn1Tag.OctetString, ConstructType.Primitive, Asn1Class.Universal));

            var sysUpTime = new VarBind(new ObjectIdentifier("1.3.6.1.2.1.1.3.0"),
                506009u,
                new Asn1TagInfo(Asn1SnmpTag.TimeTicks));

            var trapVb = new VarBind(new ObjectIdentifier("1.3.6.1.6.3.1.1.4.1.0"),
                new ObjectIdentifier("1.3.6.1.4.1.500.12"),
                new Asn1TagInfo(Asn1Tag.ObjectIdentifier, ConstructType.Primitive, Asn1Class.Universal));

            var packet = new SnmpDatagram(
                PduType.SNMPv2Trap,
                SnmpVersion.V2C,
                "Community",
                50000,
                SnmpErrorStatus.NoError,
                0,
                new[] { sysUpTime, trapVb, octetStringVarBind });

            var encoded = packet.ToSnmpEncodedByteArray();

            var udpDatagram = new UdpDatagram(this.fakeIpPacket, 10, 10, (ushort)(encoded.Length + 8), encoded);

            var transformedOutput = transform(udpDatagram) as FakeTrap2;

            Assert.IsNotNull(transformedOutput);
            Assert.IsNotNull(transformedOutput.Property);
  
            Assert.AreEqual(payload.Length, transformedOutput.Property.Length);
            Assert.IsTrue(payload.Zip(transformedOutput.Property, (b, b1) => b == b1).All(i => i));
        }

        [TestMethod]
        public void Test_Enum()
        {
            var typeMap = new TrapTypeMap();

            var transform = typeMap.GetTransform(typeof(FakeTrap3));

            Assert.IsNotNull(transform);

            var integerVarBind = new VarBind(
                new ObjectIdentifier("1.3.6.1.4.1.562.29.6.1.1.1.6"),
                1L,
                new Asn1TagInfo(Asn1Tag.Integer, ConstructType.Primitive, Asn1Class.Universal));

            var sysUpTime = new VarBind(new ObjectIdentifier("1.3.6.1.2.1.1.3.0"),
                506009u,
                new Asn1TagInfo(Asn1SnmpTag.TimeTicks));

            var trapVb = new VarBind(new ObjectIdentifier("1.3.6.1.6.3.1.1.4.1.0"),
                new ObjectIdentifier("1.3.6.1.4.1.500.12"),
                new Asn1TagInfo(Asn1Tag.ObjectIdentifier, ConstructType.Primitive, Asn1Class.Universal));

            var packet = new SnmpDatagram(
                PduType.SNMPv2Trap,
                SnmpVersion.V2C,
                "Community",
                50000,
                SnmpErrorStatus.NoError,
                0,
                new[] { sysUpTime, trapVb, integerVarBind });

            var encoded = packet.ToSnmpEncodedByteArray();

            var udpDatagram = new UdpDatagram(this.fakeIpPacket, 10, 10, (ushort)(encoded.Length + 8), encoded);

            var transformedOutput = transform(udpDatagram) as FakeTrap3;

            Assert.IsNotNull(transformedOutput);
            Assert.AreEqual(SimpleEnum.B, transformedOutput.EnumProperty);
        }

        [SnmpTrap("1.3.6.1.4.1.500.12")]
        internal class FakeTrap2
        {
            [SnmpOid("1.3.6.1.4.1.562.29.6.2.2")]
            public byte[] Property { get; set; }

            [SnmpOid("1.3.6.1.4.1.562.29.6.2.2")]
            public string StringProperty { get; set; }
        }

        [SnmpTrap("1.3.6.1.4.1.500.12")]
        internal class FakeTrap3
        {
            [SnmpOid("1.3.6.1.4.1.562.29.6.1.1.1.6")]
            public SimpleEnum EnumProperty { get; set; }
        }

        public enum SimpleEnum
        {
            A = 0,
            B = 1,
            C = 2,
        };

        [SnmpTrap("1.3.6.1.4.1.500.12")]
        internal class FakeTrap
        {
            [SnmpOid("1.3.6.1.2.1.1.3.0")]
            public uint SysUpTime { get; set; }

            [SnmpOid("1.3.6.1.4.1.1.1.1")]
            public long Integer { get; set; }

            [IpAddress]
            public IPAddress SourceAddress { get; set; }

            [NotificationObjects]
            public ReadOnlyCollection<VarBind> Objects { get; set; }

            [Timestamp]
            public DateTimeOffset ReceivedTime { get; set; }
        }

        [SnmpTrap("1.3.6.1.4.1.500.12")]
        internal class FakeTrapStringIp
        {
            [IpAddress]
            public string SourceAddress { get; set; }
        }

        internal class UnmarkedTrap
        {
            [SnmpOid("1.3.6.1.2.1.1.3.0")]
            public uint SysUpTime { get; set; }
        }
    }
}
