# Desenvolvimento e publicação

Repositório: <https://github.com/MiguelSMaciel/Trajeto-Inclusivo>.

## Preparar o projeto

Clone o repositório e abra a pasta raiz no Unity Hub com Unity **6000.6.4f1**. Os manifests em `Packages` registram as dependências; a primeira abertura pode precisar de internet para restaurá-las. Os mapas e as três cenas estão versionados e funcionam sem serviço geográfico externo.

Instale no Hub o módulo da plataforma desejada: Windows Build Support ou Linux Build Support (Mono). Os menus **Trajeto Inclusivo → Gerar executável Windows/Linux x86_64** produzem as pastas correspondentes em `Builds`. Para os comandos completos e o empacotamento Linux, consulte [LINUX.md](LINUX.md).

Não versione `Library`, `Temp`, `Logs`, `Builds`, `Distribution`, `.cache`, `.tools` nem `TestResults`. Versione os arquivos `.meta` junto com seus assets: eles preservam as referências das cenas. Não regenere cenas geográficas para uma alteração de documentação ou interface.

## Verificar alterações

Use **Window → General → Test Runner** e execute EditMode e PlayMode. A validação do executável usa `-stage4-smoke`, percorre as três cenas e salva relatórios e capturas. Os resultados efetivamente obtidos estão em [VALIDACAO.md](VALIDACAO.md). Dados do OSM e experiências artificiais devem continuar identificados separadamente na interface e nos documentos.

## Publicar Linux

1. Gere o executável com `TrajetoInclusivo.Editor.LinuxBuild.Build`.
2. Execute `pwsh -File Tools/Package-Linux.ps1` (PowerShell 7.4 ou superior). O arquivo tar.gz preserva as permissões de execução Linux; o script também gera `SHA256SUMS.txt`.
3. Crie uma versão no GitHub vinculada ao commit dos fontes e anexe os dois arquivos de `Distribution`. Não envie binários ao histórico Git.
4. Na aba **Actions**, execute **Validar executável Linux**, informando a tag publicada. O job Ubuntu 22.04 baixa exatamente o pacote da versão, confere seu checksum e executa a matriz com Xvfb/Mesa. Os relatórios ficam no artifact `linux-native-results` por 14 dias.
5. Registre a execução e os limites da validação em `Docs/VALIDACAO.md`. Esse teste verifica execução gráfica por software; testes em GPUs e distribuições diferentes continuam úteis.

O workflow testa o player já compilado e não precisa de licença do Unity Editor. A compilação pelo Editor continua sendo feita em uma instalação licenciada do Unity com o módulo de destino.

## Documentação e licenças

[README](../README.md) contém uso e controles; [LINUX.md](LINUX.md) oferece o índice dos guias técnicos. `NOTICE.md` e os créditos dos snapshots acompanham a distribuição. Os dados OSM e suas derivações seguem ODbL 1.0; não atribua essa licença automaticamente ao código ou ao Unity. Uma licença para o código autoral ainda deve ser escolhida pelo titular.
