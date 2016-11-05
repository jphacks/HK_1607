class SendExpressionDataJob < ApplicationJob
  queue_as :default

  # 理解度の値を先生宛てで送信
  def perform(data)
    # 登録されている先生全てに対する処理
    User.where(teacher_flag: true).each do |teacher|
      # 理解度の値を通知
      LearningChannel.broadcast_to(teacher, {
        expression_data: data
      })
    end
  end
end
