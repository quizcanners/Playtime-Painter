using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;



namespace StoryTriggerData {

    public static class SpaceDistanceExtensions {
        public static void TransferToSmallerScale(this Vector3 a, Vector3 o, float coefficient) {
            float d = a.x % 1;
            o.x += d * coefficient;
            a.x -= d;

            d = a.y % 1;
            o.y += d * coefficient;
            a.y -= d;

            d = a.z % 1;
            o.z += d * coefficient;
            a.z -= d;
        }


        public static void TransferToLargerScale(this Vector3 a, Vector3 o, float coefficient) {
            float d = (int)(a.x / coefficient);
            a.x -= d * coefficient;
            o.x += d;

            d = (int)(a.y / coefficient);
            a.y -= d * coefficient;
            o.y += d;

            d = (int)(a.z / coefficient);
            a.z -= d * coefficient;
            o.z += d;
        }

    }


    [Serializable]
    public class UniverseLength : abstract_STD {
        public static UniverseLength one = new UniverseLength(1);

        public float spM;
        public float spKM;
        public float spLM;
        public float spLY;
        public float spMP;
 
    public void Multiply (UniverseLength o) {
        spM *= o.spM;
        spKM *= o.spKM;
        spLM *= o.spLM;
        spLY *= o.spLY;
        spMP *= spMP;
    }

        public override void Decode(string tag, string data) {

            switch (tag) {
                case "MP": spMP = data.ToInt(); break;
                case "LY": spLY = data.ToInt(); break;
                case "LM": spLM = data.ToInt(); break;
                case "KM": spKM = data.ToInt(); break;
                case "M": spM = data.ToInt(); break;
                default: Debug.Log(tag + "Not recognized"); break;
            }

        }

        public override stdEncoder Encode() {

            var cody = new stdEncoder();

            cody.AddIfNotZero("MP", spMP, 0.0001f);
            cody.AddIfNotZero("LY", spLY, 0.0001f);
            cody.AddIfNotZero("LM", spLM, 0.0001f);
            cody.AddIfNotZero("KM", spKM, 0.0001f);
            cody.AddIfNotZero("M", spM, 0.0001f);

            return cody;
        }

        public static bool expand;

        public override void PEGI() {

            if (expand) {
                pegi.newLine();
                if (pegi.Click("Collapse"))
                    expand = false;
                pegi.newLine();
                pegi.write("M");
                pegi.edit(ref spM);
                pegi.newLine();
                pegi.write("KM");
                pegi.edit(ref spKM);
                pegi.newLine();
                pegi.write("LM");
                pegi.edit(ref spLM);
                pegi.newLine();
                pegi.write("LY");
                pegi.edit(ref spLY);
                pegi.newLine();
                pegi.write("MP");
                pegi.edit(ref spMP);
            } else {
                pegi.write("SpaceLength");
                if (pegi.Click("Expand"))
                    expand = true;
            }
            pegi.newLine();
        }

        public override string getDefaultTagName() {
            return "SpacePos";
        }

        void TransferToLargerScale(ref float smaller, ref float larger, float coeficient) {
            float d = (int)(smaller / coeficient);
            smaller -= d * coeficient;
            larger += d;
        }

        public void TransferToSmallerScale(ref float larger, ref float smaller, float coefficient) {
            float d = larger % 1;
            smaller += d * coefficient;
            larger -= d;
        }

        public void AdjustValues() {
            TransferToSmallerScale(ref spMP, ref spLY, UniversePosition.lightYearsInMegaparsec);
            TransferToSmallerScale(ref spLY, ref spLM, UniversePosition.minutesInYear);
            TransferToSmallerScale(ref spLM, ref spKM, UniversePosition.L_Speed_KMperMIN);
            TransferToSmallerScale(ref spKM, ref spM, UniversePosition.tylesInKilometer);

            TransferToLargerScale(ref spM, ref spKM, UniversePosition.tylesInKilometer);
            TransferToLargerScale(ref spKM, ref spLM, UniversePosition.L_Speed_KMperMIN);
            TransferToLargerScale(ref spLM, ref spLY, UniversePosition.minutesInYear);
            TransferToLargerScale(ref spLY, ref spMP, UniversePosition.lightYearsInMegaparsec);
        }

        public bool biggerThen(UniverseLength o) {
            if (spMP != o.spMP) return spMP > o.spMP;
            if (spLY != o.spLY) return spLY > o.spLY;
            if (spLM != o.spLM) return spLM > o.spLM;
            if (spKM != o.spKM) return spKM > o.spKM;
            if (spM != o.spM) return spM > o.spM;

            return false;
        }

        public void CopyFrom(UniverseLength sd) {
            spM = sd.spM;
            spKM = sd.spKM;
            spLM = sd.spLM;
            spLY = sd.spLY;
            spMP = sd.spMP;
        }

 
        public void Zero() {
            spM = 0;
            spKM = 0;
            spLM = 0;
            spLY = 0;
            spMP = 0;
        }

        public override string ToString() {
            return " Distance: " + spMP + " MP, " + spLY + " LY, " + spLM + " LS, " + spKM + " KM " + spM + " T";
        }

        public void DebugWrite(string tag) {
            Debug.Log(tag + ToString());
        }

        public UniverseLength(UniverseLength from) {
            CopyFrom(from);
        }

        public UniverseLength(string data) {
            Reboot(data);
        }

        public override void Reboot(string data) {
            Zero();
            base.Reboot(data);
        }



        public UniverseLength(int T) {
            spM = T;
            AdjustValues();
        }

        public UniverseLength() {

        }
    }

    [Serializable]
    public class UniversePosition : abstract_STD {

        public override void Decode(string tag, string data) {

            switch (tag) {
                case "MP": posMP = data.ToVector3(); break;
                case "LY": posMP = data.ToVector3(); break;
                case "LM": posMP = data.ToVector3(); break;
                case "KM": posMP = data.ToVector3(); break;
                case "M": posMP = data.ToVector3(); break;
                default: Debug.Log(tag + "Not recognized"); break;
            }

        }

        public override void Reboot(string data) {
            Zero();
            base.Reboot(data);
        }

        public override stdEncoder Encode() {

            var cody = new stdEncoder();

            cody.AddIfNotZero("MP", posMP);
            cody.AddIfNotZero("LY", posLY);
            cody.AddIfNotZero("LM", posLM);
            cody.AddIfNotZero("KM", posKM);
            cody.AddIfNotZero("M", posM);

            return cody;
        }

        public const string storyTag = "SpacePos";

        public override string getDefaultTagName() {
            return storyTag;
        }

        public const float accuracyLimit = 2048;

        public static float lightYearsInMegaparsec = 3260000;
        public static float minutesInYear = 525600;        //31536000;
        public static float L_Speed_KMperMIN = 299792;        //2458; // meters
        public static float tylesInKilometer = 1000;          //299792458; // meters

        public static UniversePosition playerPosition = new UniversePosition();
        public static float MaxFarPlane = 1000f;

        public static UniversePosition tmpsPos = new UniversePosition();
        public static UniverseLength tmpDist = new UniverseLength();

        //the speed of light = 299 792 458 m / s
        // light year 9,4607 × 10(15) m 
        // float max 10(38)
        // megaparsec = million parsecs, 3.3 light-years = parsec


        public Vector3 posMP; // mogapasecs
        public Vector3 posLY; // light years
        public Vector3 posLM; // light mштгеуі
        public Vector3 posKM; // kilometers
        public Vector3 posM; // meters / tyles

        


        public bool expand = false;

        public override string ToString() {
            string tmp = "";

            if (posMP.magnitude > 0) tmp += posMP.ToString() + " Megaparsecs ";
            else {
                if (posLY.magnitude > 0) tmp += posLY.ToString() + " light years ";
                if (posLY.magnitude < 5) {
                    if (posLM.magnitude > 0) tmp += posLM.ToString() + " light minutes ";
                    if (posLM.magnitude < 5) tmp += posKM.ToString() + " km " + posM.ToString() + " tyles ";
                }
            }
            return tmp;
        }

        public void AdjustValues() {
            posMP.TransferToSmallerScale(posLY, lightYearsInMegaparsec);
            posLY.TransferToSmallerScale(posLM, minutesInYear);
            posLM.TransferToSmallerScale(posKM, L_Speed_KMperMIN);
            posKM.TransferToSmallerScale(posM, tylesInKilometer);

            posM.TransferToLargerScale(posKM, tylesInKilometer);
            posKM.TransferToLargerScale(posLM, L_Speed_KMperMIN);
            posLM.TransferToLargerScale(posLY, minutesInYear);
            posLY.TransferToLargerScale(posMP, lightYearsInMegaparsec);
        }

        public void LerpBySpeedTo(UniverseLength speed, UniversePosition o) {
            UniverseLength sdist = tmpDist;
            Vector3 vec = CalculateDistAndVectorTo(sdist, o);

            float dist = sdist.spMP;
            float sp = speed.spMP * Time.deltaTime;
            float way = Mathf.Min(dist, sp);
            posMP += (vec * way);

            dist = sdist.spLY + Mathf.Min(1, dist - way) * lightYearsInMegaparsec;
            sp = speed.spLY * Time.deltaTime + Mathf.Min(1, sp - way) * lightYearsInMegaparsec;
            way = Mathf.Min(dist, sp);
            posLY += (vec * way);

            dist = sdist.spLM + Mathf.Min(1, dist - way) * minutesInYear;
            sp = speed.spLM * Time.deltaTime + Mathf.Min(1, sp - way) * minutesInYear;
            way = Mathf.Min(dist, sp);
            posLM += (vec * way);

            dist = sdist.spKM + Mathf.Min(1, dist - way) * L_Speed_KMperMIN;
            sp = speed.spKM * Time.deltaTime + Mathf.Min(1, sp - way) * L_Speed_KMperMIN;
            way = Mathf.Min(dist, sp);
            posKM += (vec * way);

            dist = sdist.spM + Mathf.Min(1, dist - way) * tylesInKilometer;
            sp = speed.spM * Time.deltaTime + Mathf.Min(1, sp - way) * tylesInKilometer;
            way = Mathf.Min(dist, sp);
            posM += (vec * way);

            AdjustValues();
        }



        public void LerpTo(UniversePosition spos, UniverseLength rad, float Portion) {

            Portion = Mathf.Clamp01(Portion);

            UniverseLength sdist = tmpDist;
            Vector3 vec = CalculateDistAndVectorTo(sdist, spos);

            sdist.spM = Mathf.Max(0, sdist.spM - rad.spM * 2.2f);
            sdist.spKM = Mathf.Max(0, sdist.spKM - rad.spKM * 2.2f);

            float solidMP = (int)(sdist.spMP / 1024) * 1024;
            float solidLY = (int)(sdist.spLY / 1024) * 1024;
            float solidLM = (int)(sdist.spLM / 1024) * 1024;
            float solidKM = (int)(sdist.spKM / 1024) * 1024;

            posMP += (vec * solidMP * Portion);
            posLY += (vec * ((sdist.spMP - solidMP) * lightYearsInMegaparsec + solidLY) * Portion);
            posLM += (vec * ((sdist.spLY - solidLY) * minutesInYear + solidLM) * Portion);
            posKM += (vec * ((sdist.spLM - solidLM) * L_Speed_KMperMIN + solidKM) * Portion);
            posM += (vec * ((sdist.spKM - solidKM) * tylesInKilometer + sdist.spM) * Portion);

            AdjustValues();

        }




        public static UniverseLength tmpScale = new UniverseLength();

        public static bool isInside;
        public Vector3 toV3(UniverseLength size, out float scale, UniverseLength dist) {

            UniversePosition upos = playerPosition;
            float distance01 = 1;
            float farPlane = MaxFarPlane;

            Vector3 tmp = upos.CalculateDistAndVectorTo(dist, this);

            isInside = size.biggerThen(dist);

            if (isInside) {
                scale = 1;
                return tmp;
            }

            scale = size.spMP;
            float fullDist = dist.spMP;

            bool near = false;

            if (dist.spMP < accuracyLimit) {
                fullDist = fullDist * lightYearsInMegaparsec + dist.spLY;
                scale = scale * lightYearsInMegaparsec + size.spLY;
                if (fullDist < accuracyLimit) {
                    fullDist = fullDist * minutesInYear + dist.spLM;
                    scale = scale * minutesInYear + size.spLM;

                    if (fullDist < accuracyLimit) {
                        near = true;
                        fullDist = ((fullDist * L_Speed_KMperMIN + dist.spKM) * tylesInKilometer + dist.spM);
                        scale = ((scale * L_Speed_KMperMIN + size.spKM) * tylesInKilometer + size.spM);
                    }
                }
            }

            if (near) {
                scale = fullDist > farPlane ? (scale * farPlane / Mathf.Max(1, fullDist)) : scale;
                distance01 = Mathf.Min(fullDist / farPlane, 1);
            } else
                scale = scale / fullDist;



            // Uncomment and provide Quaternion to rotate the universe around center. (If you want to rotate stars around planet, for example.) 
            // tmp = universeRotation * tmp;  
            


            tmp = tmp.normalized * farPlane * distance01;
            return tmp;
        }


        public Vector3 DirectionTo(UniversePosition o) {

            Vector3 diffMP = o.posMP - posMP;

            Vector3 diff = diffMP;

            float power = 1 / (1 + diffMP.magnitude * lightYearsInMegaparsec);

            Vector3 diffLY = o.posLY - posLY;

            diff += diffLY * power;

            power /= (1 + diffLY.magnitude * minutesInYear);

            Vector3 diffLS = o.posLM - posLM;

            diff += diffLS * power;

            power /= (1 + posLM.magnitude * L_Speed_KMperMIN);

            Vector3 diffKM = o.posKM - posKM + diffLS * L_Speed_KMperMIN;

            diff += diffKM * power;

            power /= (1 + diffKM.magnitude * tylesInKilometer);

            Vector3 diffM = o.posM - posM;

            diff += diffM * power;

            return diff.normalized;
        }


        public Vector3 DistAndDirectionTo(UniverseLength sd, UniversePosition o) {
            sd.Zero();

            Vector3 dir = new Vector3();

            Vector3 diffMP = o.posMP - posMP;

            float magn = diffMP.magnitude;

            if (magn < accuracyLimit) {
                Vector3 diffLY = o.posLY - posLY + diffMP * lightYearsInMegaparsec;
                magn = diffLY.magnitude;

                if (magn < accuracyLimit) {
                    Vector3 diffLS = o.posLM - posLM + diffLY * minutesInYear;
                    magn = diffLS.magnitude;

                    if (magn < accuracyLimit) {
                        Vector3 diffKM = o.posKM - posKM + diffLS * L_Speed_KMperMIN;
                        magn = diffKM.magnitude;

                        if (magn < accuracyLimit) {
                            Vector3 diffT = o.posM - posM + diffKM * tylesInKilometer;
                            sd.spM = diffT.magnitude;
                            dir = diffT;
                        } else { sd.spKM = magn; dir = diffKM; }
                    } else { sd.spLM = magn; dir = diffLS; }
                } else { sd.spLY = magn; dir = diffLY; }
            } else { sd.spMP = magn; dir = diffMP; }

            sd.AdjustValues();

            return dir.normalized;
        }

        public void CopyFrom(UniversePosition o) {
            posM = o.posM;
            posKM = o.posKM;
            posLM = o.posLM;
            posLY = o.posLY;
            posMP = o.posMP;
        }

        public void Zero() {
            posM = Vector3.zero;
            posKM = Vector3.zero;
            posLM = Vector3.zero;
            posLY = Vector3.zero;
            posMP = Vector3.zero;
        }

        public UniversePosition() {

            if ((playerPosition != null) && (playerPosition != this))
                CopyFrom(playerPosition);
        }

        public UniversePosition GetCopy() {
            UniversePosition tmp = new UniversePosition();
            tmp.posMP = posMP;
            tmp.posLY = posLY;
            tmp.posLM = posLM;
            tmp.posKM = posKM;
            tmp.posM = posM;
            return tmp;
        }

        public override void PEGI() {

            if (expand) {
                pegi.newLine();
                if (pegi.Click("Collapse"))
                    expand = false;
                pegi.newLine();
                pegi.write("M");
                pegi.edit(ref posM);
                pegi.newLine();
                pegi.write("KM");
                pegi.edit(ref posKM);
                pegi.newLine();
                pegi.write("LM");
                pegi.edit(ref posLM);
                pegi.newLine();
                pegi.write("LY");
                pegi.edit(ref posLY);
                pegi.newLine();
                pegi.write("MP");
                pegi.edit(ref posMP);
            } else {
                pegi.write("SpacePos", 50);
                if (pegi.Click("Expand"))
                    expand = true;
            }
        }

        public float FullTransformUpdate(Transform tf, Transform meshTF, UniverseLength radius, UniverseLength dist) {

            // Note: This update only clamps object to maxDistance for coordinate. Objects insode plane each manage their FarPlane positioning. 

            float scale;
            tf.localPosition = toV3(radius, out scale, dist);
            meshTF.localScale = new Vector3(scale, scale, scale);

            //Uncomment to rotate objects around center.
            //tf.rotation = globUniverse.universeRotation;


            return scale;

        }

    }
}
