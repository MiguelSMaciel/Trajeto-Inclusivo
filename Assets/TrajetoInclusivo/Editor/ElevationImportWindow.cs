using UnityEditor;
using UnityEngine;

namespace TrajetoInclusivo.Editor
{
    public sealed class ElevationImportWindow:EditorWindow
    {
        private TextAsset csv;
        private string source="",datum="",date="",message="";
        private double verticalOrigin;
        private bool simulated;
        [MenuItem("Trajeto Inclusivo/Importar grade CSV de cotas locais")]
        private static void Open()=>GetWindow<ElevationImportWindow>("Grade de elevação");
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("CSV com x,z,elevation, em metros. X/Z devem usar a mesma origem local da cena. Não há reprojeção automática ou inferência de datum. Cotas importadas não validam acessibilidade.",MessageType.Info);
            csv=(TextAsset)EditorGUILayout.ObjectField("CSV local",csv,typeof(TextAsset),false);
            source=EditorGUILayout.TextField("Fonte / URL",source);datum=EditorGUILayout.TextField("Datum vertical",datum);
            date=EditorGUILayout.TextField("Data dos dados",date);verticalOrigin=EditorGUILayout.DoubleField("Origem vertical (m)",verticalOrigin);
            simulated=EditorGUILayout.Toggle("Dados simulados",simulated);
            if(GUILayout.Button("Validar e salvar asset") && csv!=null)
            {
                ElevationDataset data=null;
                try
                {
                    data=ElevationDataImporter.ParseCsv(csv.text,source,datum,verticalOrigin,simulated);data.sourceDate=date;
                    string path=EditorUtility.SaveFilePanelInProject("Salvar grade","ElevationGrid","asset","Salvar a grade validada");
                    if(path.Length>0){AssetDatabase.CreateAsset(data,path);message="Grade importada; atribua ao RuntimeWorldEditor durante Play ou carregue o CSV no painel.";data=null;}
                }
                catch(System.Exception e){message=e.Message;}
                finally{if(data!=null)DestroyImmediate(data);}
            }
            EditorGUILayout.HelpBox(message,MessageType.None);
        }
    }
}
