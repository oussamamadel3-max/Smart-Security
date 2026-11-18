import 'package:flutter/material.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

enum InfoNoticeTone { info, warning }

/// Shared notice banner used across device/history screens.
class InfoNotice extends StatelessWidget {
  const InfoNotice({
    super.key,
    required this.text,
    this.tone = InfoNoticeTone.info,
  });

  final String text;
  final InfoNoticeTone tone;

  @override
  Widget build(BuildContext context) {
    final warning = tone == InfoNoticeTone.warning;
    final bg = warning ? DesignTokens.noticeBg : DesignTokens.infoSoftBg;
    final fg = warning ? DesignTokens.noticeText : DesignTokens.infoSoftText;
    return Container(
      width: double.infinity,
      margin: const EdgeInsets.only(bottom: DesignTokens.spaceMd),
      padding: const EdgeInsets.all(DesignTokens.spaceMd),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(DesignTokens.radiusMd),
        border: Border.all(
          color: warning ? DesignTokens.noticeBorder : DesignTokens.border,
        ),
      ),
      child: Text(
        text,
        style: Theme.of(context).textTheme.bodySmall?.copyWith(color: fg),
      ),
    );
  }
}
