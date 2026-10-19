# Evidências preservadas

`Windows` contém os XMLs NUnit da última validação da navegação (59 testes EditMode e 10 PlayMode), a matriz CSV de 60 combinações e o relatório do player Windows. São registros da execução descrita em `Docs/VALIDACAO.md`, anterior ao port; as mudanças de port concentram-se em build, distribuição e documentação.

Os arquivos `Windows/publicacao-2026-10-19-player.csv` e `.txt` registram uma nova execução de 60/60 casos do programa extraído do ZIP Windows preparado para publicação. Os XMLs NUnit anteriores permanecem como histórico; não foram reexecutados para essa alteração de empacotamento.

`Linux` contém a matriz de 60 combinações, relatório, ambiente, log do player e 13 capturas da execução nativa aprovada em Ubuntu 22.04.5. O workflow e os limites estão descritos em `Docs/VALIDACAO.md`. O manifesto `player-files-sha256.txt` identifica os binários e dados internos do player. `tested-package-sha256.txt` identifica o tar.gz usado na primeira execução; o pacote foi depois atualizado com esta documentação e as evidências, preservando os arquivos do player.

Os diretórios `TestResults` e `Builds` são saídas locais e não fazem parte do histórico Git. Os relatórios selecionados aqui permitem consultar as evidências mesmo após um clone limpo. As capturas mostram simulação; **© OpenStreetMap contributors — ODbL 1.0**.
