using MischievousByte.Masquerade;
using MischievousByte.Masquerade.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
                //public Vector3 scapula;
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
                //scapula = new(0.27f, 0.83f, -0.02f),
                upperArm = new(0.29f, 0.8f, 0.015f),
                hand = new()
                {
                    length = 0.2f / 1.76f,
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
            var vectors = new BodyTree<Vector3>();

            GenerateSpineAndHead(in data.measurements, in data.properties, ref vectors);
            GenerateArms(in data.measurements, in data.properties, ref vectors);

            tree = new();
            foreach (BodyNode node in BodyNode.All.Enumerate())
                tree[node] = Matrix4x4.Translate(vectors[node]);
            
            tree.ChangeSpace(Space.Self, out tree);
        }

        private static void GenerateSpineAndHead(in BodyMeasurements measurements, in Properties properties, ref BodyTree<Vector3> tree)
        {
            float spineLength = properties.spine.length * measurements.height;
            float headLength = properties.head.length * measurements.height;
            float headDepth = headLength * properties.head.depth;

            float headY = measurements.height + headLength * (-0.5f + properties.head.connection.y);


            tree[BodyNode.Sacrum] = new(0, headY - spineLength, 0); //z defaults to zero, might add property for it
            tree[BodyNode.L3] = new(0, Mathf.Lerp(tree[BodyNode.Sacrum].y, headY, properties.spine.vertebrae.l3.y), spineLength * properties.spine.vertebrae.l3.x);
            tree[BodyNode.T12]= new(0, Mathf.Lerp(tree[BodyNode.Sacrum].y, headY, properties.spine.vertebrae.t12.y), spineLength * properties.spine.vertebrae.t12.x);
            tree[BodyNode.T7] = new(0, Mathf.Lerp(tree[BodyNode.Sacrum].y, headY, properties.spine.vertebrae.t7.y), spineLength * properties.spine.vertebrae.t7.x);
            tree[BodyNode.C7] = new(0, Mathf.Lerp(tree[BodyNode.Sacrum].y, headY, properties.spine.vertebrae.c7.y), spineLength * properties.spine.vertebrae.c7.x);

            tree[BodyNode.Head] = new(0, headY, spineLength * properties.spine.skull);
            tree[BodyNode.Eyes] = new(0, measurements.height - headLength / 2, tree[BodyNode.Head].z + tree[BodyNode.Head].z - properties.head.connection.x * headDepth + 0.5f * headDepth);
            tree[BodyNode.HeadTop] = new Vector3(0, measurements.height, tree[BodyNode.Head].z + tree[BodyNode.Head].z - properties.head.connection.x * headDepth);
        }

        private static void GenerateArms(in BodyMeasurements measurements, in Properties properties, ref BodyTree<Vector3> tree)
        {
            float spineLength = properties.spine.length * measurements.height;

            tree[BodyNode.RightClavicle] = tree[BodyNode.Sacrum] + properties.arm.clavicle * spineLength;
            //tree[BodyNode.RightScapula] = tree[BodyNode.Sacrum] + properties.arm.scapula * spineLength;
            tree[BodyNode.RightUpperArm] = tree[BodyNode.Sacrum] + properties.arm.upperArm * spineLength;

            float handLength = measurements.wingspan * properties.arm.hand.length;
            float armLength = measurements.wingspan * 0.5f - tree[BodyNode.RightUpperArm].x;
            float segmentLength = (armLength - handLength) / 2f;

            tree[BodyNode.RightForearm]  = tree[BodyNode.RightUpperArm] + Vector3.right * segmentLength;
            tree[BodyNode.RightWrist] = tree[BodyNode.RightForearm] + Vector3.right * segmentLength;
            tree[BodyNode.RightHand] = tree[BodyNode.RightWrist] + properties.arm.hand.palm * handLength;

            Matrix4x4 mirror = Matrix4x4.Scale(new Vector3(-1, 1, 1));
            tree[BodyNode.LeftClavicle] = mirror * tree[BodyNode.RightClavicle];
            //tree[BodyNode.LeftScapula] = mirror * tree[BodyNode.RightScapula];
            tree[BodyNode.LeftUpperArm] = mirror * tree[BodyNode.RightUpperArm];
            tree[BodyNode.LeftForearm] = mirror * tree[BodyNode.RightForearm];
            tree[BodyNode.LeftWrist] = mirror * tree[BodyNode.RightWrist];
            tree[BodyNode.LeftHand] = mirror * tree[BodyNode.RightHand];
        }
    }
}