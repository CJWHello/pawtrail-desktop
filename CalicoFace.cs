using System;
using System.Windows;
using System.Windows.Media;

namespace PawTrail
{
    public partial class PetArt
    {
        // The two portraits share the same markings. Register the traced face to
        // the seated pose using its ear tips, eye corners, nose and cheek tufts.
        // Hit testing uses the inverse of this same transform.
        static readonly Matrix FacePlacement = new Matrix(
            1.29244, .02706, -.03375, 1.28846, -241.99, 3.16);
        static readonly MatrixTransform portraitPlacement = MakePortraitPlacement();

        static MatrixTransform MakePortraitPlacement()
        {
            var transform = new MatrixTransform(FacePlacement);
            transform.Freeze();
            return transform;
        }

        const string PortraitTopEdge =
            "M569,422 C586,422 598,412 605,402 L593,409 C605,399 611,382 614,366 " +
            "C619,344 617,324 628,292 C615,250 607,182 609,133 C609,113 609,95 615,84 " +
            "C617,79 622,79 628,81 C662,93 721,133 755,163 C803,141 855,134 908,146 " +
            "C938,109 985,58 1009,45 C1012,43 1017,41 1020,43 C1034,53 1046,94 1050,130 " +
            "C1055,164 1056,201 1055,230 C1078,256 1088,286 1097,319 " +
            "C1104,342 1112,360 1130,370 L1121,370 L1133,374";
        const string PortraitOutline = PortraitTopEdge +
            " C1126,394 1104,411 1087,414 L1104,423 C1093,434 1081,439 1061,442 L1069,445 " +
            "C1048,457 1028,465 1009,479 C987,493 964,507 937,515 " +
            "C909,524 868,529 831,526 C791,525 760,522 735,515 C715,509 690,499 667,492 " +
            "C646,486 626,481 612,470 Q629,463 637,453 C611,454 585,440 569,422 Z";
        const string OrangeMask =
            "M593,65 L865,80 L852,164 L842,195 Q835,212 831,222 L830,214 " +
            "Q824,222 821,235 L820,224 Q814,232 811,247 L811,238 " +
            "Q807,247 806,260 L805,254 Q801,265 803,276 L801,271 " +
            "Q800,285 805,300 L803,295 Q804,308 802,318 " +
            "C814,343 801,363 785,378 C772,386 762,390 751,396 L758,390 " +
            "C740,403 716,415 693,420 L703,414 C689,422 662,430 642,429 " +
            "Q656,424 665,414 Q650,422 641,421 C653,408 664,397 674,386 " +
            "Q649,406 631,413 Q645,402 650,390 C637,403 618,415 604,418 " +
            "Q617,410 621,406 C602,415 583,422 569,423 L568,65 Z";
        const string DarkMask =
            "M781,130 L1051,22 L1137,245 L1146,370 Q1123,379 1103,371 L1114,377 " +
            "Q1102,376 1095,375 L1100,380 Q1088,382 1079,377 L1085,383 " +
            "Q1070,389 1055,389 L1063,392 C1045,396 1023,396 1003,391 " +
            "L1009,396 C981,392 964,381 949,374 L956,381 C937,373 924,360 911,347 " +
            "C908,336 905,322 898,308 L899,318 Q893,311 888,297 L889,305 " +
            "Q881,291 880,281 L880,291 Q874,281 870,262 L869,269 " +
            "C860,255 856,235 854,216 L852,228 Q850,214 853,200 " +
            "Q841,209 837,221 L838,210 Q831,216 829,228 L829,216 Q821,221 816,232 " +
            "Q814,219 813,209 C801,224 798,230 793,243 L793,232 " +
            "Q786,244 785,252 C781,242 782,229 787,216 Q779,230 780,240 " +
            "C779,214 794,184 806,163 L799,172 Z";
        const string LeftEyeSocket = "M698,321 C715,300 739,293 759,296 C784,298 798,312 804,329 C806,350 790,370 772,376 C746,384 721,372 710,356 Q700,342 698,321 Z";
        const string RightEyeSocket = "M907,321 C913,296 938,278 961,275 C980,272 997,277 1007,287 C1007,312 995,338 973,350 C950,363 927,358 914,344 Q907,336 907,321 Z";

        static LinearGradientBrush PortraitWash(string first, string second, string third, double middle, Point start, Point end)
        {
            var brush = new LinearGradientBrush { StartPoint = start, EndPoint = end };
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(first), 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(second), middle));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(third), 1));
            brush.Freeze();
            return brush;
        }

        static readonly Brush portraitIvory = PortraitWash("#FFFCF6", "#FFF6E8", "#F6EADB", .68, new Point(.35,0),new Point(.6,1));
        static readonly Brush portraitApricot = PortraitWash("#F3B46C", "#ECAB63", "#D9975A", .61,new Point(.74,.03),new Point(.12,1));
        static readonly Brush portraitCoal = PortraitWash("#282526", "#383230", "#322A29", .48,new Point(.72,.06),new Point(.31,.96));
        static readonly Brush portraitEar = PortraitWash("#D7948F", "#E8AFAB", "#E6B8AD", .65,new Point(.38,0),new Point(.57,1));
        static readonly Brush portraitGold = PortraitWash("#AA7B3F", "#F3C969", "#FFE8A5", .28,new Point(.35,0),new Point(.61,1));
        static readonly Brush portraitNose = PortraitWash("#F8BCB7", "#ECA3A3", "#C98288", .65,new Point(.4,0),new Point(.6,1));

        void DrawPortrait(DrawingContext dc, Point pupilOffset)
        {
            dc.PushTransform(portraitPlacement);
            Geometry head = Shape(PortraitOutline);
            dc.DrawGeometry(portraitIvory, null, head);
            dc.PushClip(head);
            dc.DrawGeometry(portraitApricot, null, Shape(OrangeMask));
            dc.DrawGeometry(portraitCoal, null, Shape(DarkMask));
            // A few longer guard hairs break the otherwise solid mask edges.
            P(dc,"M786,147 Q805,144 820,142 Q810,156 797,167 L803,155 Q793,169 783,176 Q789,162 792,154 L784,161 Z", "#DC9754");
            P(dc,"M814,146 Q821,143 829,141 L817,159 L818,152 L810,165 Z","#C7884D");
            P(dc,"M627,349 Q637,340 648,337 Q635,347 626,360 L639,354 Q631,360 621,373 Z M640,384 Q651,369 659,368 L647,381 L658,376 Q651,384 639,389 Z","#22BC8047");
            P(dc,"M596,428 C617,428 635,420 647,411 Q635,428 623,434 Q636,433 644,429 Q633,440 615,443 Z M618,462 Q635,459 650,451 Q644,461 634,467 L654,463 L640,472 Z", "#58C7AF97");
            P(dc,"M616,472 C644,479 655,479 676,477 L669,482 Q690,481 704,476 L694,486 " +
                "C718,482 732,482 751,481 L743,488 Q759,492 778,491 L765,497 Q777,499 790,498 " +
                "Q813,502 830,501 L825,508 Q843,510 857,504 L853,511 Q871,509 885,503 L877,511 " +
                "Q897,509 907,503 L899,512 Q928,506 948,494 L940,502 Q974,485 990,476 L985,482 " +
                "C1004,474 1023,458 1036,453 L1034,458 Q1055,445 1084,428 L1112,423 L1039,488 L954,536 L810,543 L666,513 Z","#55CDBDAE");
            P(dc,"M772,509 Q792,517 812,516 L803,520 L825,517 L820,521 Q837,520 852,516 L848,521 L880,517 L871,522 Q898,519 915,511 L911,517 L938,510 L915,526 L800,537 Z","#69CDBEB1");
            P(dc,"M1070,387 Q1096,388 1118,379 Q1108,388 1095,393 L1104,393 Q1093,398 1081,399 L1085,395 L1067,399 Z","#55CEBEB0");
            P(dc,"M641,430 Q659,421 674,405 Q667,418 659,424 L668,421 L655,429 Z M681,426 Q700,417 710,408 Q704,418 681,426 Z","#78FBDFC0");
            P(dc,"M1000,383 C1012,387 1024,390 1036,391 Q1027,391 1021,390 L1032,394 Q1015,392 1000,383 Z M1048,394 Q1061,392 1077,389 Q1071,392 1060,395 Z","#88615851");
            P(dc,"M946,181 Q932,198 927,210 M760,227 Q772,242 777,255 M1053,403 Q1061,402 1069,398",null,"#48EBD9BF",.85);
            dc.Pop();

            // The outer line narrows to fur marks at the chin, as on the reference.
            P(dc,PortraitTopEdge,null,"#211918",4.4);
            P(dc,"M1131,376 C1128,392 1109,406 1088,414 L1103,423 Q1087,436 1062,442 L1070,445 C1038,459 1003,481 977,494",null,"#2B211E",3.8);
            P(dc,"M571,422 Q594,452 635,453 L613,470 Q625,480 667,491 C691,499 714,509 737,515",null,"#2B211E",4);
            P(dc,"M737,515 Q749,520 760,521 M967,500 L977,494",null,"#69564D",1.45);
            DrawPortraitEars(dc);
            dc.DrawGeometry(furGrain, null, head);

            double cycle = ((phase % 15.0) + 15.0) % 15.0;
            double blink = Clamp(1 - Math.Abs(cycle - 8.2) / .41, 0, 1);
            double shut = Math.Max(blink, Math.Max(petting, doze));
            DrawPortraitEye(dc,false,shut,pupilOffset);
            DrawPortraitEye(dc,true,shut,pupilOffset);

            // Nose bridge, blush and tiny split upper lip.
            dc.PushTransform(new TranslateTransform(2.6,-1.6));
            P(dc,"M843,360 Q848,349 859,347 Q874,344 886,356 Q878,353 867,355 Q852,357 843,360 Z","#40EFC8B8");
            dc.DrawGeometry(portraitNose,null,Shape("M842,369 Q849,365 863,364 Q876,363 882,367 Q883,372 873,378 L864,389 Q861,391 859,385 L849,377 Q840,374 842,369 Z"));
            P(dc,"M842,373 Q845,376 851,378 M875,375 Q878,373 881,370",null,"#764649",1.9);
            P(dc,"M863,389 Q865,398 863,404",null,"#A27769",2);
            P(dc,"M863,404 C858,413 846,426 836,424 Q830,425 829,421 M864,404 C869,412 877,418 890,419 Q899,420 903,414",null,"#49332C",2.35);
            dc.Pop();
            DrawPortraitWhiskers(dc);

            if (skin == 1) DrawPortraitHat(dc);
            if (skin == 2) DrawPortraitBerry(dc);
            dc.Pop();
        }

        static void DrawPortraitEars(DrawingContext dc)
        {
            dc.DrawGeometry(portraitEar,null,Shape("M621,97 Q622,91 630,96 C654,109 704,164 721,190 C697,184 680,183 669,186 Q682,194 686,203 L650,203 L663,215 Q648,221 638,239 C625,205 615,133 621,97 Z"));
            P(dc,"M621,120 Q619,103 622,94 C647,97 698,158 720,189",null,"#754E40",2.3);
            P(dc,"M618,123 Q623,154 631,181 L633,172 Q638,193 649,204 L640,203 Q633,202 627,193 Q629,219 637,233 C630,216 622,188 620,171 Z","#35BA756B");
            P(dc,"M638,245 Q644,229 653,220 L645,223 Q654,214 664,211 L651,202 L677,203 L670,195 L692,198 L684,189 Q703,189 716,193 L705,186 L719,190 Q696,181 678,184 L663,185 L672,195 L645,201 L655,212 Q644,216 638,226 Z","#FFF5E5");
            P(dc,"M956,176 C967,145 995,83 1014,58 Q1018,54 1021,59 C1037,84 1044,137 1044,174 Q1044,199 1040,216 Q1032,204 1025,198 L1026,203 Q1018,193 1007,188 L1027,182 L1002,179 L1011,163 L990,176 L998,159 L979,170 L986,155 Z","#B48C82");
            dc.DrawGeometry(portraitEar,null,Shape("M959,172 Q986,96 1016,58 Q1019,56 1021,60 C1038,94 1046,155 1043,191 L1041,213 L1033,199 L1033,204 Q1022,194 1008,186 L1026,182 L1002,180 L1010,164 L991,176 L998,159 L980,171 L984,157 Z"));
            P(dc,"M958,171 Q983,100 1016,57 Q1018,54 1021,59 C1037,93 1047,169 1040,212",null,"#412D2C",2.35);
            P(dc,"M1007,184 L1023,181 L1017,188 L1029,190 L1026,194 Q1035,200 1039,208 Q1025,197 1011,192 L1015,194 L993,188 Z","#F7E2D7");
            P(dc,"M639,237 Q646,226 651,223 M987,171 L994,165",null,"#FFF1E3",1.6);
            // Fine pale streaks follow the growth direction of the forehead fur.
            P(dc,"M754,182 Q773,209 785,249 M733,192 Q761,213 777,249 M749,219 Q766,236 779,259",null,"#CFfff1d7",1.5);
            P(dc,"M930,162 Q916,193 911,228 M958,159 Q935,190 917,231 M946,201 Q928,213 915,237",null,"#B9F6EAD9",1.3);
        }

        void DrawPortraitEye(DrawingContext dc,bool right,double shut,Point pupilOffset)
        {
            string socket = right ? RightEyeSocket : LeftEyeSocket;
            if (shut > .93)
            {
                P(dc,right ? "M907,320 C928,315 962,298 980,289 Q996,281 1008,283" :
                    "M687,320 Q716,310 744,315 C768,318 791,330 804,336",null,"#2B201D",5);
                return;
            }
            dc.PushTransform(new ScaleTransform(1,1 - .93 * shut,right ? 956 : 752,right ? 316 : 328));
            Geometry aperture = Shape(socket);
            dc.DrawGeometry(portraitGold,null,aperture);
            dc.PushClip(aperture);
            if (right)
            {
                P(dc,"M908,315 Q927,283 961,278 Q981,273 1004,286 L1016,311 Q1004,293 985,289 Q946,283 908,322 Z","#7AA97E48");
                P(dc,"M916,329 Q925,353 944,357 L932,356 Q919,350 911,339 Z","#E3AD56");
            }
            else
            {
                P(dc,"M698,320 C715,296 746,289 768,299 Q782,303 794,318 Q777,308 756,307 Q722,309 705,339 Z","#78997856");
                P(dc,"M702,330 Q702,346 715,359 Q724,369 732,372 Q719,371 711,363 L699,348 Z","#FBE6B1");
            }
            dc.PushTransform(new TranslateTransform(pupilOffset.X,pupilOffset.Y));
            double cx = right ? 953 : 763, cy = right ? 312 : 333;
            dc.PushTransform(new RotateTransform(right ? -12 : -6,cx,cy));
            E(dc,cx,cy,right ? 22 : 21.4,right ? 37.8 : 35.6,"#27201B");
            dc.Pop();
            E(dc,right ? 939.4 : 749,right ? 297.8 : 314.5,right ? 6.9 : 7.4,7.5,"#FFFEF9");
            dc.Pop();
            dc.Pop();
            P(dc,socket,null,"#332419",2.25);
            if (right)
                P(dc,"M904,325 C905,306 920,285 939,276 C960,265 980,265 996,272 Q1004,276 1013,276 L1007,281 L1024,282 L1014,290 Q1011,319 996,333 C1007,308 1007,294 1001,286 C985,275 963,274 945,281 C924,289 913,303 907,326 Z","#191310");
            else
                P(dc,"M680,319 Q687,317 694,312 C715,293 738,286 761,292 C783,296 800,307 807,328 C800,316 784,304 761,300 C737,296 719,303 705,318 Q700,333 713,355 Q698,340 692,324 Z","#1C1410");
            dc.Pop();
        }

        static void DrawPortraitWhiskers(DrawingContext dc)
        {
            P(dc,"M728,397 C668,374 604,369 547,390 Q623,364 728,395 Z","#D8C3B2");
            P(dc,"M727,401 C655,384 594,403 548,446 C595,401 655,383 727,399 Z","#E5D3C2");
            P(dc,"M728,397 C688,382 650,377 610,377 Q666,376 728,395 Z M727,401 Q669,385 617,401 Q662,384 727,399 Z","#FFF9EB");
            P(dc,"M996,369 C1066,337 1124,326 1176,333 C1124,324 1065,335 996,367 Z","#DCCEC2");
            P(dc,"M1001,383 C1063,367 1129,359 1183,395 C1128,356 1060,366 1001,381 Z","#DDCFC3");
            P(dc,"M996,369 Q1070,334 1134,332 Q1073,331 996,367 Z M1001,383 Q1078,363 1127,370 Q1081,360 1001,381 Z","#FFF7E6");
        }

        static void DrawPortraitHat(DrawingContext dc)
        {
            dc.PushTransform(new RotateTransform(-7,844,143));
            P(dc,"M878,138 Q906,132 932,143 L921,149 L927,161 Q905,152 879,155 Z","#48799B","#443936",3.1);
            P(dc,"M774,142 C766,134 765,121 778,111 C788,92 807,87 824,91 C846,75 869,82 877,89 C899,88 913,101 914,115 Q926,130 913,141 L892,149 L780,152 Z","#F6F5EB","#443936",3.7);
            P(dc,"M775,138 Q844,122 913,133 L910,150 Q854,140 783,156 Z","#5887A5","#443936",3);
            P(dc,"M802,118 Q805,105 814,101 M888,102 Q900,105 901,117",null,"#C7D4D2",3);
            E(dc,847,111,3,3,null,"#557B8C",2);
            P(dc,"M847,114 L847,130 M837,121 Q839,130 847,130 Q856,130 858,121",null,"#557B8C",2.9);
            dc.Pop();
        }

        static void DrawPortraitBerry(DrawingContext dc)
        {
            dc.PushTransform(new RotateTransform(13,1048,230));
            P(dc,"M1029,226 Q1008,207 1004,222 Q1003,240 1025,242 L1038,232 Z M1067,226 Q1090,207 1095,220 Q1100,237 1082,244 L1064,239 Z","#EBA1A8","#643F40",3);
            P(dc,"M1024,224 Q1018,206 1037,208 Q1047,207 1052,211 Q1067,200 1076,213 C1089,229 1062,254 1051,260 Q1046,261 1038,248 Q1029,240 1024,224 Z","#CA5F69","#573A36",3.4);
            P(dc,"M1044,211 L1037,198 Q1049,199 1053,207 Q1058,195 1067,199 L1060,208 L1074,209 L1061,216 L1054,212 L1046,220 L1046,215 L1035,217 Z","#75917B","#4F5144",2);
            P(dc,"M1033,224 L1035,228 M1044,228 L1045,232 M1059,221 L1059,225 M1069,222 L1067,226 M1053,239 L1051,243 M1044,239 L1045,242",null,"#FDE6B5",2.3);
            dc.Pop();
        }
    }
}
