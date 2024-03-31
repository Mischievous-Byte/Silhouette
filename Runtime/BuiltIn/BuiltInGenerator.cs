using MischievousByte.Masquerade;
using MischievousByte.Masquerade.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MischievousByte.Silhouette.BuiltIn
{
    public static class BuiltInGenerator
    {
        [System.Serializable]
        public struct Properties
        {
            [System.Serializable]
            public struct Spine
            {
                public float length;
                public (Vector2 l3, Vector2 t12, Vector2 t7, Vector2 c7) vertebrae;
                public float skull;
            }


            [System.Serializable]
            public struct Head
            {
                public float length;
                public float depth;
                public Vector2 connection;
            }

            [System.Serializable]
            public struct Arm
            {
                public Vector3 clavicle;
                public Vector3 scapula;
                public Vector3 upperArm;
                public (float length, Vector3 palm) hand;
            }


            public Spine spine;
            public Head head;
            public Arm arm;
        }


        public static readonly Properties DefaultProperties = new()
        {
            spine = new()
            {
                length = 0.4f,
                vertebrae =
                (
                    new(0.05f, 0.157f),
                    new(0, 0.37f),
                    new(-0.075f, 0.59f),
                    new(0, 0.88f)
                ),
                skull = 0.059f
            },
            head = new()
            {
                length = 0.225f / 1.76f,
                depth = 0.8f,
                connection = new(-0.25f, -0.4f),
            },
            arm = new()
            {
                clavicle = new(0.03f, 0.86f, 0.18f),
                scapula = new(0.14f, 0.65f, -0.1f),
                upperArm = new(0.29f, 0.8f, 0.015f),
                hand = new()
                {
                    length = 0.2f / 1.75f,
                    palm = new(0.3f, -0.075f, 0f)
                }
            }
        };



        [SkeletonGenerator]
        public static void Generate(in BodyMeasurements measurements, out BodyTree<Matrix4x4> tree)
        {
            Generate((DefaultProperties, measurements), out tree);
        }

        [SkeletonGenerator]
        public static void Generate(in (Properties properties, BodyMeasurements measurements) data, out BodyTree<Matrix4x4> tree)
        {
            tree = new();
            foreach (BodyNode node in BodyNode.All.Enumerate())
                tree[node] = Matrix4x4.identity;
                

            GenerateSpineAndHead(in data.measurements, in data.properties, ref tree);
            GenerateArms(in data.measurements, in data.properties, ref tree);
            //GenerateLegs(in data.measurements, in data.properties, ref tree);
        }

        
        private static void GenerateSpineAndHead(in BodyMeasurements measurements, in Properties properties, ref BodyTree<Matrix4x4> tree)
        {
            float spineLength = properties.spine.length * measurements.height;
            float headLength = properties.head.length * measurements.height;
            float headDepth = headLength * properties.head.depth;
            
            float headY = measurements.height + headLength * (-0.5f + properties.head.connection.y);

            Vector3 sacrumPosition = new(0, headY - spineLength, 0); //z defaults to zero, might add property for it
            Vector3 l3Position = new(0, Mathf.Lerp(sacrumPosition.y, headY, properties.spine.vertebrae.l3.y), spineLength * properties.spine.vertebrae.l3.x);
            Vector3 t12Position = new(0, Mathf.Lerp(sacrumPosition.y, headY, properties.spine.vertebrae.t12.y), spineLength * properties.spine.vertebrae.t12.x);
            Vector3 t7Position = new(0, Mathf.Lerp(sacrumPosition.y, headY, properties.spine.vertebrae.t7.y), spineLength * properties.spine.vertebrae.t7.x);
            Vector3 c7Position = new(0, Mathf.Lerp(sacrumPosition.y, headY, properties.spine.vertebrae.c7.y), spineLength * properties.spine.vertebrae.c7.x);

            Vector3 headPosition = new(0, headY, spineLength * properties.spine.skull);
            Vector3 eyesPosition = new(0, headY - headLength / 2, headPosition.z + headPosition.z - properties.head.connection.x * headDepth + 0.5f * headDepth);
            Vector3 headTopPosition = new Vector3(0, measurements.height, headPosition.z + headPosition.z - properties.head.connection.x * headDepth);


            Matrix4x4 worldSacrum = Matrix4x4.Translate(sacrumPosition);
            Matrix4x4 worldL3 = Matrix4x4.Translate(l3Position);
            Matrix4x4 worldT12 = Matrix4x4.Translate(t12Position);
            Matrix4x4 worldT7 = Matrix4x4.Translate(t7Position);
            Matrix4x4 worldC7 = Matrix4x4.Translate(c7Position);

            Matrix4x4 worldHead = Matrix4x4.Translate(headPosition);
            Matrix4x4 worldEyes = Matrix4x4.Translate(eyesPosition);
            Matrix4x4 worldHeadTop = Matrix4x4.Translate(headTopPosition);

            tree[BodyNode.Sacrum] = worldSacrum;
            tree[BodyNode.L3] = worldSacrum.inverse * worldL3;
            tree[BodyNode.T12] = worldL3.inverse * worldT12;
            tree[BodyNode.T7] = worldT12.inverse * worldT7;
            tree[BodyNode.C7] = worldT7.inverse * worldC7;
            tree[BodyNode.Head] = worldC7.inverse * worldHead;
            tree[BodyNode.Eyes] = worldHead.inverse * worldEyes;
            tree[BodyNode.HeadTop] = worldHead.inverse * worldHeadTop;
        }



        private static void GenerateArms(in BodyMeasurements measurements, in Properties properties, ref BodyTree<Matrix4x4> tree)
        {
            float spineLength = properties.spine.length * measurements.height;

            Vector3 claviclePosition = properties.arm.clavicle * spineLength;
            Vector3 scapulaPosition = properties.arm.scapula * spineLength;
            Vector3 upperArmPosition = properties.arm.scapula * spineLength;

            float handLength = measurements.wingspan * properties.arm.hand.length;
            float armLength = measurements.wingspan * 0.5f - upperArmPosition.x;
            float segmentLength = (armLength - handLength) / 2f;

            Vector3 forearmPosition = upperArmPosition + Vector3.right * segmentLength;
            Vector3 wristPosition = forearmPosition + Vector3.right * segmentLength;
            Vector3 handPosition = wristPosition + properties.arm.hand.palm * handLength;

            Matrix4x4 nonLocalClavicle = Matrix4x4.Translate(claviclePosition);
            Matrix4x4 nonLocalScapula = Matrix4x4.Translate(scapulaPosition);
            Matrix4x4 nonLocalUpperArm = Matrix4x4.Translate(upperArmPosition);
            Matrix4x4 nonLocalForearm = Matrix4x4.Translate(forearmPosition);
            Matrix4x4 nonLocalWrist = Matrix4x4.Translate(wristPosition);
            Matrix4x4 nonLocalHand = Matrix4x4.Translate(handPosition);

            tree[BodyNode.RightClavicle] = nonLocalClavicle;
            tree[BodyNode.RightScapula] = nonLocalClavicle.inverse * nonLocalScapula;
            tree[BodyNode.RightUpperArm] = nonLocalScapula.inverse * nonLocalUpperArm;
            tree[BodyNode.RightForearm] = nonLocalUpperArm.inverse * nonLocalForearm;
            tree[BodyNode.RightWrist] = nonLocalForearm.inverse * nonLocalWrist;
            tree[BodyNode.RightHand] = nonLocalWrist.inverse * nonLocalHand;


            Matrix4x4 MirrorX(in Matrix4x4 m) => Matrix4x4.Translate(new Vector3(-m.GetPosition().x, m.GetPosition().y, m.GetPosition().z));

            tree[BodyNode.LeftClavicle] = MirrorX(tree[BodyNode.RightClavicle]);
            tree[BodyNode.LeftScapula] = MirrorX(tree[BodyNode.RightScapula]);
            tree[BodyNode.LeftUpperArm] = MirrorX(tree[BodyNode.RightUpperArm]);
            tree[BodyNode.LeftForearm] = MirrorX(tree[BodyNode.RightForearm]);
            tree[BodyNode.LeftWrist] = MirrorX(tree[BodyNode.RightWrist]);
            tree[BodyNode.LeftHand] = MirrorX(tree[BodyNode.RightHand]);
        }
    }
}