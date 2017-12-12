using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
using System.Text;

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

    public abstract class SpaceValues : abstract_STD {
        public const float accuracyLimit = 2048;

        public static float lightYearsInMegaparsec = 3260000;
        public static float minutesInYear = 525600;        //31536000;
        public static float L_Speed_KMperMIN = 299792;        //2458; 
        public static float maters_In_Kilometer = 1000;          //299792458; // meters

        public static UniversePosition playerPosition = new UniversePosition();
        public static UniverseLength universeScale = new UniverseLength(1);
        public static float FarPlane = 1000f;

        protected const string meters = "meters";
        protected const string kilometers = "kilometers";
        protected const string lightMinutes = "light minutes";
        protected const string lightYears = "light years";
        protected const string megaParsecs = "mega parsecs";
        //const string 
        public static bool autoUniverseScale = true;
        public static bool expand = false;

        public int scaleLevel = 0;

    }


    [Serializable]
    public class UniverseLength : SpaceValues
    {
        public static UniverseLength one = new UniverseLength(1);

        public float Meters;
        public float KM;
        public float LM;
        public float LY;
        public float MP;
 
        public bool Equals(int i) {
            return (MP == 0 && LY == 0 && LM == 0 && KM == 0 && Meters == i);
        }


    public void Divide (float val){
        Meters/=val;
        KM/=val;
        LM/=val;
        LY/=val;
        MP/=val;
    }

 
    public void Divide (UniverseLength o) { 
   
      float div = (o.MP* UniversePosition.lightYearsInMegaparsec + o.LY);
      
      if (div < UniversePosition.accuracyLimit)
            {
        div = (div* UniversePosition.minutesInYear + o.LM);
        if (div<accuracyLimit){
            div = (div*L_Speed_KMperMIN + o.KM);
            
           // if (div<accuracyLimit){
             
                div = (div*maters_In_Kilometer + o.Meters);
                Divide(div);
           
           
         } else {
            Meters = MP*lightYearsInMegaparsec/div*minutesInYear +  (LY*minutesInYear + LM)/div;
            MP=LY=LM=KM = 0;
         }
       } else {
            Meters = (MP*lightYearsInMegaparsec+LY)/div;
            MP=LY=LM=KM = 0;
       }
  
        /*spM /= o.spM;
        spKM /= o.spKM;
        spLM /= o.spLM;
        spLY /= o.spLY;
        spMP /= spMP;*/
        
               AdjustValues();
    }
        
        public override void Decode(string tag, string data) {

            switch (tag) {
                case "MP": MP = data.ToFloat(); break;
                case "LY": LY = data.ToFloat(); break;
                case "LM": LM = data.ToFloat(); break;
                case "KM": KM = data.ToFloat(); break;
                case "M": Meters = data.ToFloat(); break;
                default: Debug.Log(tag + "Not recognized"); break;
            }

        }

        public override stdEncoder Encode() {

            var cody = new stdEncoder();

            cody.AddIfNotZero("MP", MP, 0.0001f);
            cody.AddIfNotZero("LY", LY, 0.0001f);
            cody.AddIfNotZero("LM", LM, 0.0001f);
            cody.AddIfNotZero("KM", KM, 0.0001f);
            cody.AddIfNotZero("M", Meters, 0.0001f);

            return cody;
        }

        public bool PEGIbase()
        {
            bool changed = false;


            changed |= "M".edit(ref Meters).nl();
            changed |= "KM".edit(ref KM).nl();
            changed |= "LM".edit(ref LM).nl();
            changed |= "LY".edit(ref LY).nl();
            changed |= "MP".edit(ref MP).nl();

            return changed;
        }

        public override bool PEGI() {
            bool changed = false;
            if (expand)
            {
                pegi.newLine();
                if (icon.Close.Click(20).nl())
                    expand = false;

                changed = PEGIbase();

            }
            else
            {
                pegi.write("SpacePos", 50);
                if (icon.Edit.Click(20))
                    expand = true;
            }
            return changed;
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

        public UniverseLength AdjustValues() {
            TransferToSmallerScale(ref MP, ref LY, UniversePosition.lightYearsInMegaparsec);
            TransferToSmallerScale(ref LY, ref LM, UniversePosition.minutesInYear);
            TransferToSmallerScale(ref LM, ref KM, UniversePosition.L_Speed_KMperMIN);
            TransferToSmallerScale(ref KM, ref Meters, UniversePosition.maters_In_Kilometer);

            TransferToLargerScale(ref Meters, ref KM, UniversePosition.maters_In_Kilometer);
            TransferToLargerScale(ref KM, ref LM, UniversePosition.L_Speed_KMperMIN);
            TransferToLargerScale(ref LM, ref LY, UniversePosition.minutesInYear);
            TransferToLargerScale(ref LY, ref MP, UniversePosition.lightYearsInMegaparsec);

            return this;
        }

        public bool biggerThen(UniverseLength o) {
            if (MP != o.MP) return MP > o.MP;
            if (LY != o.LY) return LY > o.LY;
            if (LM != o.LM) return LM > o.LM;
            if (KM != o.KM) return KM > o.KM;
            if (Meters != o.Meters) return Meters > o.Meters;

            return false;
        }

        public UniverseLength CopyFrom(UniverseLength sd) {
            Meters = sd.Meters;
            KM = sd.KM;
            LM = sd.LM;
            LY = sd.LY;
            MP = sd.MP;
            return this;
        }

        public UniverseLength Zero() {
            Meters = 0;
            KM = 0;
            LM = 0;
            LY = 0;
            MP = 0;
            return this;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            if (MP > 0) sb.Append(MP+" MP, ");
            if (LY > 0) sb.Append(LY + " LY, ");
            if (LM > 0) sb.Append(LM + " LM, ");
            if (KM > 0) sb.Append(KM + " KM, ");
            if (Meters > 0) sb.Append(Meters + " Meters, ");


            return sb.ToString();
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
            Meters = T;
            AdjustValues();
        }

        public UniverseLength() {

        }
    }

    [Serializable]
    public class UniversePosition : SpaceValues
    {

        public override void Decode(string tag, string data) {

            switch (tag) {
                case "MP": MP = data.ToVector3(); break;
                case "LY": LY = data.ToVector3(); break;
                case "LM": LM = data.ToVector3(); break;
                case "KM": KM = data.ToVector3(); break;
                case "M": Meters = data.ToVector3(); break;
                default: Debug.Log(tag + "Not recognized"); break;
            }

        }

        public override void Reboot(string data) {
            Zero();
            base.Reboot(data);
        }

        public override stdEncoder Encode() {

            var cody = new stdEncoder();

            cody.AddIfNotZero("MP", MP);
            cody.AddIfNotZero("LY", LY);
            cody.AddIfNotZero("LM", LM);
            cody.AddIfNotZero("KM", KM);
            cody.AddIfNotZero("M", Meters);

            return cody;
        }

        public const string storyTag = "SpacePos";

        public override string getDefaultTagName() {
            return storyTag;
        }

    

        public static UniversePosition tmpsPos = new UniversePosition();
        public static UniverseLength tmpDist = new UniverseLength();
        public static UniverseLength tmpScale = new UniverseLength();
        //the speed of light = 299 792 458 m / s
        // light year 9,4607 × 10(15) m 
        // float max 10(38)
        // megaparsec = million parsecs, 3.3 light-years = parsec


        public Vector3 MP; // mogapasecs
        public Vector3 LY; // light years
        public Vector3 LM; // light mштгеуі
        public Vector3 KM; // kilometers
        public Vector3 Meters; // meters / tyles
        

        public override string ToString() {
            string tmp = "";

            if (MP.magnitude > 0) tmp += MP.ToStringShort() + " MP, ";
            else {
                if (LY.magnitude > 0) tmp += LY.ToStringShort() + " LY, ";
                if (LY.magnitude < 5) {
                    if (LM.magnitude > 0) tmp += LM.ToStringShort() + " LM, ";
                    if (KM.magnitude > 0) tmp += KM.ToStringShort() + " Km, ";
                    if (Meters.magnitude > 0) tmp += Meters.ToStringShort() + " m. ";
                }
            }
            return tmp;
        }

        public void AdjustValues() {
            MP.TransferToSmallerScale(LY, lightYearsInMegaparsec);
            LY.TransferToSmallerScale(LM, minutesInYear);
            LM.TransferToSmallerScale(KM, L_Speed_KMperMIN);
            KM.TransferToSmallerScale(Meters, maters_In_Kilometer);

            Meters.TransferToLargerScale(KM, maters_In_Kilometer);
            KM.TransferToLargerScale(LM, L_Speed_KMperMIN);
            LM.TransferToLargerScale(LY, minutesInYear);
            LY.TransferToLargerScale(MP, lightYearsInMegaparsec);
        }

        public void LerpBySpeedTo(UniverseLength speed, UniversePosition o) {
            UniverseLength sdist = tmpDist;
            Vector3 vec = DistAndDirectionUnscaledTo(sdist, o);

            float dist = sdist.MP;
            float sp = speed.MP * Time.deltaTime;
            float way = Mathf.Min(dist, sp);
            MP += (vec * way);

            dist = sdist.LY + Mathf.Min(1, dist - way) * lightYearsInMegaparsec;
            sp = speed.LY * Time.deltaTime + Mathf.Min(1, sp - way) * lightYearsInMegaparsec;
            way = Mathf.Min(dist, sp);
            LY += (vec * way);

            dist = sdist.LM + Mathf.Min(1, dist - way) * minutesInYear;
            sp = speed.LM * Time.deltaTime + Mathf.Min(1, sp - way) * minutesInYear;
            way = Mathf.Min(dist, sp);
            LM += (vec * way);

            dist = sdist.KM + Mathf.Min(1, dist - way) * L_Speed_KMperMIN;
            sp = speed.KM * Time.deltaTime + Mathf.Min(1, sp - way) * L_Speed_KMperMIN;
            way = Mathf.Min(dist, sp);
            KM += (vec * way);

            dist = sdist.Meters + Mathf.Min(1, dist - way) * maters_In_Kilometer;
            sp = speed.Meters * Time.deltaTime + Mathf.Min(1, sp - way) * maters_In_Kilometer;
            way = Mathf.Min(dist, sp);
            Meters += (vec * way);

            AdjustValues();
        }
        
        public void LerpTo(UniversePosition spos, UniverseLength rad, float Portion) {

            Portion = Mathf.Clamp01(Portion);

            UniverseLength sdist = tmpDist;
            Vector3 vec = DistAndDirectionUnscaledTo(sdist, spos);

            sdist.Meters = Mathf.Max(0, sdist.Meters - rad.Meters * 2.2f);
            sdist.KM = Mathf.Max(0, sdist.KM - rad.KM * 2.2f);

            float solidMP = (int)(sdist.MP / 1024) * 1024;
            float solidLY = (int)(sdist.LY / 1024) * 1024;
            float solidLM = (int)(sdist.LM / 1024) * 1024;
            float solidKM = (int)(sdist.KM / 1024) * 1024;

            MP += (vec * solidMP * Portion);
            LY += (vec * ((sdist.MP - solidMP) * lightYearsInMegaparsec + solidLY) * Portion);
            LM += (vec * ((sdist.LY - solidLY) * minutesInYear + solidLM) * Portion);
            KM += (vec * ((sdist.LM - solidLM) * L_Speed_KMperMIN + solidKM) * Portion);
            Meters += (vec * ((sdist.KM - solidKM) * maters_In_Kilometer + sdist.Meters) * Portion);

            AdjustValues();

        }




        public static bool isInside;
        public static bool isInReach;
        public Vector3 ToV3(UniverseLength scale, UniverseLength reach, out float size) {
            
       
            float distance01 = 1;
            size = 1;

            Vector3 tmp = playerPosition.DistAndDirectionUnscaledTo(tmpDist, this);

            isInside = scale.biggerThen(tmpDist);
            isInReach = reach.biggerThen(tmpDist);

            tmpScale.CopyFrom(scale).Divide(universeScale);
            tmpDist.Divide(universeScale);
            
            if (isInside) 
                return tmp * (tmpDist.Meters + tmpDist.KM * maters_In_Kilometer);
            
            size = tmpScale.MP;
            float fullDist = tmpDist.MP;
            bool near = false;

            if (tmpDist.MP < accuracyLimit) {
                fullDist = fullDist * lightYearsInMegaparsec + tmpDist.LY;
                size = size * lightYearsInMegaparsec + tmpScale.LY;
                if (fullDist < accuracyLimit) {
                    fullDist = fullDist * minutesInYear + tmpDist.LM;
                    size = size * minutesInYear + tmpScale.LM;

                    if (fullDist < accuracyLimit) {
                        near = true;
                        fullDist = ((fullDist * L_Speed_KMperMIN + tmpDist.KM) * maters_In_Kilometer + tmpDist.Meters);
                        size = ((size * L_Speed_KMperMIN + tmpScale.KM) * maters_In_Kilometer + tmpScale.Meters);
                    }
                }
            }

            if (near) {
                size = fullDist > FarPlane ? (size * FarPlane / Mathf.Max(1, fullDist)) : size;
                distance01 = Mathf.Min(fullDist / FarPlane, 1);
            }
            else 
                size /= fullDist;
            
            return tmp.normalized * FarPlane * distance01;
        }

        public void DistanceTo(UniverseLength sd, UniversePosition o) {
            sd.Zero();

            Vector3 diffMP = o.MP - MP;

            float magn = diffMP.magnitude;

            if (magn < accuracyLimit) {
                Vector3 diffLY = o.LY - LY + diffMP * lightYearsInMegaparsec;
                magn = diffLY.magnitude;

                if (magn < accuracyLimit) {
                    Vector3 diffLS = o.LM - LM + diffLY * minutesInYear;
                    magn = diffLS.magnitude;

                    if (magn < accuracyLimit) {
                        Vector3 diffKM = o.KM - KM + diffLS * L_Speed_KMperMIN;
                        magn = diffKM.magnitude;

                        if (magn < accuracyLimit)
                            sd.Meters = (o.Meters - Meters + diffKM * maters_In_Kilometer).magnitude;
                        
                        else  sd.KM = magn; 
                    }
                    else  sd.LM = magn; 
                }
                else  sd.LY = magn;  
            }
            else  sd.MP = magn;  

            sd.AdjustValues();
        }

        public Vector3 DirectionTo(UniversePosition o) {

            Vector3 diffMP = o.MP - MP;

            Vector3 diff = diffMP;

            float power = 1 / (1 + diffMP.magnitude * lightYearsInMegaparsec);

            Vector3 diffLY = o.LY - LY;

            diff += diffLY * power;

            power /= (1 + diffLY.magnitude * minutesInYear);

            Vector3 diffLS = o.LM - LM;

            diff += diffLS * power;

            power /= (1 + LM.magnitude * L_Speed_KMperMIN);

            Vector3 diffKM = o.KM - KM + diffLS * L_Speed_KMperMIN;

            diff += diffKM * power;

            power /= (1 + diffKM.magnitude * maters_In_Kilometer);

            Vector3 diffM = o.Meters - Meters;

            diff += diffM * power;

            return diff.normalized;
        }
        
        public Vector3 DistAndDirectionUnscaledTo(UniverseLength sd, UniversePosition o) {
            sd.Zero();

            Vector3 dir = new Vector3();

            Vector3 diffMP = o.MP - MP;

            float magn = diffMP.magnitude;

            if (magn < accuracyLimit) {
                Vector3 diffLY = o.LY - LY + diffMP * lightYearsInMegaparsec;
                magn = diffLY.magnitude;

                if (magn < accuracyLimit) {
                    Vector3 diffLS = o.LM - LM + diffLY * minutesInYear;
                    magn = diffLS.magnitude;

                    if (magn < accuracyLimit) {
                        Vector3 diffKM = o.KM - KM + diffLS * L_Speed_KMperMIN;
                        magn = diffKM.magnitude;

                        if (magn < accuracyLimit) {
                            Vector3 diffT = o.Meters - Meters + diffKM * maters_In_Kilometer;
                            sd.Meters = diffT.magnitude;
                            dir = diffT;
                        } else { sd.KM = magn; dir = diffKM; }
                    } else { sd.LM = magn; dir = diffLS; }
                } else { sd.LY = magn; dir = diffLY; }
            } else { sd.MP = magn; dir = diffMP; }

            sd.AdjustValues();

            return dir.normalized;
        }

        public void CopyFrom(UniversePosition o) {
            Meters = o.Meters;
            KM = o.KM;
            LM = o.LM;
            LY = o.LY;
            MP = o.MP;
        }

        public void Zero() {
            Meters = Vector3.zero;
            KM = Vector3.zero;
            LM = Vector3.zero;
            LY = Vector3.zero;
            MP = Vector3.zero;
        }

        public UniversePosition() {

            if ((playerPosition != null) && (playerPosition != this))
                CopyFrom(playerPosition);
        }

        public UniversePosition GetCopy() {
            UniversePosition tmp = new UniversePosition();
            tmp.MP = MP;
            tmp.LY = LY;
            tmp.LM = LM;
            tmp.KM = KM;
            tmp.Meters = Meters;
            return tmp;
        }
        
        public bool PEGIbase()
        {


            bool changed = false;

            switch (scaleLevel) {

                case 0:
                    pegi.write(meters, 80);
                    if ((kilometers + ": " + KM).ClickUnfocus().nl()) scaleLevel++;
                    changed |= pegi.edit(ref Meters).nl(); break;
                case 1:
                    if (meters.ClickUnfocus(60)) scaleLevel--;
                    pegi.write(kilometers, 80);
                    if ((lightMinutes + ": " + LM).ClickUnfocus().nl()) scaleLevel++;
                    changed |= pegi.edit(ref KM).nl();
                    break;
                case 2:

                    if (kilometers.ClickUnfocus(60)) scaleLevel--;
                    pegi.write(lightMinutes, 80);
                    if ((lightYears + ": " + LY).ClickUnfocus().nl()) scaleLevel++;
                    changed |= pegi.edit(ref LM).nl();
                    break;

                case 3:
                    if (lightMinutes.ClickUnfocus(60)) scaleLevel--;
                    pegi.write(lightYears, 80);
                    if ((megaParsecs + ": " + MP).ClickUnfocus().nl()) scaleLevel++;
                    changed |= pegi.edit(ref LM).nl();
                    break;
                case 4:
                    if (lightYears.ClickUnfocus(60)) scaleLevel--;
                    megaParsecs.nl(80);
                    changed |= pegi.edit(ref MP).nl();
                    break;

            }
            return changed;
        }

        public override bool PEGI() {
            bool changed = false;
            if (expand)
            {
                pegi.newLine();
                if (icon.Close.Click(20).nl())
                    expand = false;

                changed |= PEGIbase();
                
            }
            else
            {
                pegi.write("SpacePos", 50);
                if (icon.Edit.Click(20))
                    expand = true;
            }
            return changed;
        }

        static int editing = 0;
        public bool ExtendedPEGI(UniverseLength size, UniverseLength reach) {

            bool changed = false;

            if (("Position "+ToString()).foldout(ref editing, 0).nl()) {
                changed |= PEGIbase(); 
            }
            if (("Reach " + reach.ToString()).foldout(ref editing, 1).nl()) {
                changed |= reach.PEGIbase();
            }
            
            if (("Size " + size.ToString()).foldout(ref editing, 2).nl()) {
                changed |= size.PEGIbase();
            }

            if (("Camera Pos " + playerPosition.ToString()).foldout(ref editing, 3).nl()) {
                changed |= playerPosition.PEGIbase();
            }

            if (("Universe Scale " + universeScale.ToString()).foldout(ref editing, 4).nl()) {
                changed |= universeScale.PEGIbase().nl();
                "Auto-Scale".toggle(ref autoUniverseScale);
            }

            return changed;
        }

       

    }
}
