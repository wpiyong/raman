#region copyright
/*************************************************************************
 * 
 * GEMOLOGICAL INSTITUTE OF AMERICA
 * __________________
 * 
 *  � Gemological Insitute Of America (GIA) 2018
 *  All Rights Reserved.
 * 
 * NOTICE:  All information contained herein is, and remains the property of GIA and its 
 * suppliers, if any.  The intellectual and technical concepts contained herein are 
 * proprietary to GIA and its suppliers and may be covered by U.S. and Foreign Patents,
 * patents in process, and are protected by trade secret or copyright law.
 * Dissemination of this information or reproduction of this material is strictly forbidden 
 * unless prior written permission is obtained from GIA.
 *************************************************************************/
#endregion



using SettingsLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _405Analyzer
{
    public class AppSettings : Settings
    {
        public AppSettings()
            : base("AnalyzerSettings.config")
        {
        }

        
        public double N3_START_WAVELEN { get; set; }
        public double N3_END_WAVELEN { get; set; }
        public double DIAMOND_START_WAVELEN { get; set; }
        public double DIAMOND_END_WAVELEN { get; set; }
        public double DIAMOND_N3_VIB_END_WAVELEN { get; set; }
        public double _468_START_WAVELEN { get; set; }
        public double _468_END_WAVELEN { get; set; }
        public double _468_START_WAVELEN_SMOOTH { get; set; }
        public double _468_END_WAVELEN_SMOOTH { get; set; }
        public double _490_START_WAVELEN { get; set; }
        public double _490_END_WAVELEN { get; set; }
        public double _525_START_WAVELEN { get; set; }
        public double _525_END_WAVELEN { get; set; }
        public double SIV_START_WAVELEN { get; set; }
        public double SIV_END_WAVELEN { get; set; }
        public double SIV_START_WAVELEN_SMOOTH { get; set; }
        public double SIV_END_WAVELEN_SMOOTH { get; set; }
        public double _788_START_WAVELEN { get; set; }
        public double _788_END_WAVELEN { get; set; }
        public double _788_START_WAVELEN_SMOOTH { get; set; }
        public double _788_END_WAVELEN_SMOOTH { get; set; }
        public double NI_START_WAVELEN { get; set; }
        public double NI_END_WAVELEN { get; set; }
        public double NI_START_WAVELEN_SMOOTH { get; set; }
        public double NI_END_WAVELEN_SMOOTH { get; set; }

        public double N3_MAX_FWHM { get; set; }
        public double N3_MIN_HEIGHT { get; set; }
        public double DIAMOND_MIN_HEIGHT { get; set; }
        public double _788_MIN_HEIGHT { get; set; }

        public double NI_SMOOTH_THRESHOLD { get; set; }
        public double _468_SMOOTH_THRESHOLD { get; set; }
        public double SI_SMOOTH_THRESHOLD { get; set; }

        public double DIAMOND_MAX_FWHM { get; set; }
        public double CZ_MAX_FWHM { get; set; }
        public double MOIS_MAX_FWHM { get; set; }

        public string LASER_WAVELENGTH { get; set; }

        public double N3_SIDE2_START_WAVELEN { get; set; }
        public double N3_SIDE2_END_WAVELEN { get; set; }

        public bool USE_N3_SIDE2_SATURATION { get; set; }
        public double N3_SIDE2_MIN_HEIGHT { get; set; }

        public double CZ_MIN_HEIGHT { get; set; }

        public double NI_MIN_HEIGHT { get; set; }

        public bool CHECK_RED_FL { get; set; }
            
    }
}
