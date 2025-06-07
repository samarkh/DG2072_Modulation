using System;

namespace DG2072_USB_Control.Continuous.ArbitraryWaveform.Descriptions
{
    public class MedicalDescriptions : IWaveformDescription
    {
        public bool SupportsWaveform(string waveformName)
        {
            switch (waveformName.ToUpper())
            {
                case "CARDIAC":
                case "ECG1":
                case "ECG2":
                case "ECG3":
                case "ECG4":
                case "ECG5":
                case "ECG6":
                case "ECG7":
                case "ECG8":
                case "ECG9":
                case "ECG10":
                case "ECG11":
                case "ECG12":
                case "ECG13":
                case "ECG14":
                case "ECG15":
                case "EEG":
                case "EMG":
                case "EOG":
                case "LFPULSE":
                case "TENS1":
                case "TENS2":
                case "TENS3":
                case "PULSILOGRAM":
                case "RESSPEED":
                    return true;
                default:
                    return false;
            }
        }

        public string GetBasicInfo(string waveformName)
        {
            switch (waveformName.ToUpper())
            {
                case "CARDIAC":
                    return "The Cardiac waveform simulates the electrical activity of the heart as typically seen on " +
                           "an electrocardiogram (ECG). It features the characteristic P wave, QRS complex, and T wave " +
                           "pattern that represents a complete cardiac cycle.";

                case "ECG1":
                case "ECG2":
                case "ECG3":
                case "ECG4":
                case "ECG5":
                case "ECG6":
                case "ECG7":
                case "ECG8":
                case "ECG9":
                case "ECG10":
                case "ECG11":
                case "ECG12":
                case "ECG13":
                case "ECG14":
                case "ECG15":
                    int ecgNumber = int.Parse(waveformName.ToUpper().Replace("ECG", ""));
                    return $"Electrocardiogram Pattern {ecgNumber} simulates a specific cardiac rhythm or condition " +
                           "as would be seen on a clinical ECG. These patterns are useful for testing and calibrating " +
                           "medical monitoring equipment.";

                case "EEG":
                    return "The Electroencephalogram (EEG) waveform simulates electrical activity of the brain " +
                           "as recorded by electrodes placed on the scalp. It represents typical brainwave patterns " +
                           "with characteristic frequency components.";

                case "EMG":
                    return "The Electromyogram (EMG) waveform simulates electrical activity produced by skeletal muscles. " +
                           "It represents the typical voltage patterns generated during muscle contraction and relaxation.";

                case "EOG":
                    return "The Electro-Oculogram (EOG) waveform simulates the electrical potentials caused by eye movement. " +
                           "It represents the voltage difference between the front and back of the eye as it moves.";

                // Add more medical waveform descriptions...

                default:
                    return $"The {waveformName} waveform is a medical signal pattern used in biomedical applications.";
            }
        }

        public string GetParameterInfo(string waveformName)
        {
            switch (waveformName.ToUpper())
            {
                case "CARDIAC":
                    return "Parameters:\n" +
                           "This waveform uses the basic frequency parameter to control the heart rate, with amplitude and offset parameters determining signal strength and baseline.";

                case "ECG1":
                    return "(Lead I), Bipolar limb lead measuring potential difference between right arm (-) and left arm (+), Detects left-sided heart issues and lateral wall infarctions";
                
                case "ECG2":
                    return "(Lead II), Bipolar limb lead measuring potential difference between right arm (-) and left leg (+),Best for rhythm assessment, detects inferior wall issues";
                
                case "ECG3":
                    return "(Lead III), Bipolar limb lead measuring potential difference between left arm (-) and left leg (+), Detects inferior myocardial infarction";
                
                case "ECG4":
                    return "(Lead aVR), Augmented unipolar lead viewing heart from right shoulder, Detects dextrocardia and specific arrhythmias";
                
                case "ECG5":
                    return "(Lead aVL), Augmented unipolar lead viewing heart from left shoulder, Detects high lateral wall infarctions";
                
                case "ECG6":
                    return "(Lead aVF), Augmented unipolar lead viewing heart from left foot, Important for inferior wall infarction diagnosis";
                
                case "ECG7":
                    return "(Lead V1), Precordial lead at 4th intercostal space right sternal border, Detects right bundle branch block and septal infarctions";
                
                case "ECG8":
                    return "(Lead V2), Precordial lead at 4th intercostal space left sternal border, Important for anterior wall assessment";
                
                case "ECG9":
                    return "(Lead V3), Precordial lead placed between V2 and V4, Crucial for anterior infarction detection";

                case "ECG10":
                    return "(Lead V4), Precordial lead at 5th intercostal space midclavicular line, Key for anterior/apical infarction detection";

                case "ECG11":
                    return "(Lead V5), Precordial lead at 5th intercostal space anterior axillary line, Important for lateral wall assessment";

                case "ECG12":
                    return "(Lead V6), Precordial lead at 5th intercostal space midaxillary line, Essential for lateral wall infarction diagnosis";

                case "ECG13":
                    return "(Lead V3R), Right-sided precordial lead (mirror of V3 on right chest), Used for right ventricular infarction diagnosis";

                case "ECG14":
                    return "(Lead V4R), Right-sided precordial lead (mirror of V4 on right chest), Most sensitive for right ventricular infarction";

                case "ECG15":
                    return "Parameters:\n" +
                           "This ECG pattern uses the basic frequency parameter to control the heart rate (in beats per minute). " +
                           "(Lead V7), Posterior lead at posterior axillary line same level as V4-V6, Used for posterior wall infarction detection.";

                // Add more parameter descriptions...

                default:
                    return "Use the frequency, amplitude, offset and phase controls to adjust the basic characteristics.";
            }
        }

        public string GetApplicationInfo(string waveformName)
        {
            switch (waveformName.ToUpper())
            {
                case "CARDIAC":
                    return "Applications:\n" +
                           "• Medical equipment testing and calibration\n" +
                           "• ECG monitor development\n" +
                           "• Patient monitoring simulation\n" +
                           "• Medical education and training\n" +
                           "• Heart rate variability studies";

                case "ECG1":
                case "ECG2":
                case "ECG3":
                case "ECG4":
                case "ECG5":
                case "ECG6":
                case "ECG7":
                case "ECG8":
                case "ECG9":
                case "ECG10":
                case "ECG11":
                case "ECG12":
                case "ECG13":
                case "ECG14":
                case "ECG15":
                    return "Applications:\n" +
                           "• Cardiac monitoring equipment calibration\n" +
                           "• Medical device testing for specific heart conditions\n" +
                           "• Training for medical professionals\n" +
                           "• Development of automated ECG interpretation systems\n" +
                           "• Research on cardiac signal processing algorithms";

                case "EEG":
                    return "Applications:\n" +
                           "• Neurological monitoring equipment testing\n" +
                           "• Brain-computer interface development\n" +
                           "• Neurofeedback system calibration\n" +
                           "• Sleep study equipment testing\n" +
                           "• Educational demonstrations of brain activity";

                case "EMG":
                    return "Applications:\n" +
                           "• Muscle activity monitoring equipment testing\n" +
                           "• Rehabilitation device development\n" +
                           "• Prosthetic control system testing\n" +
                           "• Neuromuscular diagnostic equipment calibration\n" +
                           "• Sports medicine research equipment testing";

                // Add more application descriptions...

                default:
                    return "Applications include biomedical equipment testing, medical education, and research in physiological signal processing.";
            }
        }

        public string GetParameterHelp(string waveformName, int paramNumber)
        {
            string paramKey = $"{waveformName.ToUpper()}_PARAM{paramNumber}";

            switch (paramKey)
            {
                // Most medical waveforms don't have additional parameters
                // But you could add them if needed

                default:
                    return "Adjust the main frequency parameter to control the rate of this medical waveform pattern.";
            }
        }
    }
}